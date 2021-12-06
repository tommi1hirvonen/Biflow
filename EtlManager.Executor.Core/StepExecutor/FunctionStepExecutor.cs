using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EtlManager.Executor.Core.StepExecutor;

internal class FunctionStepExecutor : StepExecutorBase
{
    private readonly ILogger<FunctionStepExecutor> _logger;
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;
    private readonly IExecutionConfiguration _executionConfiguration;
    private readonly IHttpClientFactory _httpClientFactory;

    private FunctionStepExecution Step { get; init; }

    private const int MaxRefreshRetries = 3;

    private JsonSerializerOptions JsonSerializerOptions { get; } = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public FunctionStepExecutor(
        ILogger<FunctionStepExecutor> logger,
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        IExecutionConfiguration executionConfiguration,
        IHttpClientFactory httpClientFactory,
        FunctionStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _executionConfiguration = executionConfiguration;
        _httpClientFactory = httpClientFactory;
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        string? functionKey = null;
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            functionKey = await context.FunctionSteps
                .AsNoTracking()
                .Select(step => step.FunctionKey)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error reading FunctionKey from database", Step.ExecutionId, Step);
        }

        var message = new HttpRequestMessage(HttpMethod.Post, Step.FunctionUrl);

        // Add function security code as a request header. If the function specific code was defined, use that.
        // Otherwise revert to the function app code if it was defined.
        functionKey ??= Step.FunctionApp.FunctionAppKey;
        if (!string.IsNullOrEmpty(functionKey))
        {
            message.Headers.Add("x-functions-key", functionKey);
        }

        // If the input for the function was defined, add it to the request content.
        if (!string.IsNullOrEmpty(Step.FunctionInput))
        {
            var input = Step.FunctionInput;
            // Iterate parameters and replace parameter names with corresponding values.
            foreach (var param in Step.StepExecutionParameters)
            {
                var value = param.ParameterValue switch
                {
                    DateTime dt => dt.ToString("o"),
                    _ => param.ParameterValue.ToString()
                };
                input = input.Replace(param.ParameterName, value);
            }
            message.Content = new StringContent(input);
        }

        using var timeoutCts = Step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                    : new CancellationTokenSource();

        var client = _httpClientFactory.CreateClient();

        HttpResponseMessage response;
        string content;

        // Send the request to the function url. This will start the function, if the request was successful.
        try
        {
            // A durable function will start immediately and return.
            if (Step.FunctionIsDurable)
            {
                response = await client.SendAsync(message, cancellationToken);
            }
            // A regular httpTrigger function can run for longer. Use an HttpClient with no timeout for httpTrigger functions.
            else
            {
                var noTimeoutClient = _httpClientFactory.CreateClient("notimeout");
                // The linked timeout token will cancel if the timeout expires or the step was canceled manually.
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                response = await noTimeoutClient.SendAsync(message, linkedCts.Token);
            }
            content = await response.Content.ReadAsStringAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return timeoutCts.IsCancellationRequested ? Result.Failure("Step execution timed out.") : Result.Failure("Step was canceled.");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error sending POST request to start function: {ex.Message}");
        }

        if (response.IsSuccessStatusCode)
        {
            var executionResult = Step.FunctionIsDurable
                ? await HandleDurableFunctionPolling(client, content, cancellationToken)
                : Result.Success(content);
            return executionResult;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
        {
            return Result.Failure("Error executing function (500 Internal Server Error)");
        }
        else
        {
            return Result.Failure($"Error sending POST request to start function: {content}");
        }
    }

    private async Task<Result> HandleDurableFunctionPolling(HttpClient client, string content, CancellationToken cancellationToken)
    {
        var startResponse = JsonSerializer.Deserialize<StartResponse>(content, JsonSerializerOptions)
                ?? throw new InvalidOperationException("Start response was null");

        using var timeoutCts = Step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                    : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Update instance id for the step execution attempt
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = Step.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == RetryAttemptCounter);
            if (attempt is not null && attempt is FunctionStepExecutionAttempt function)
            {
                function.FunctionInstanceId = startResponse.Id;
                context.Attach(function);
                context.Entry(function).Property(e => e.FunctionInstanceId).IsModified = true;
                await context.SaveChangesAsync(CancellationToken.None);
            }
            else
            {
                throw new InvalidOperationException("Could not find step execution attempt to update function instance id");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating function instance id", Step.ExecutionId, Step);
        }

        StatusResponse status;
        while (true)
        {
            try
            {
                status = await TryGetStatusAsync(client, startResponse.StatusQueryGetUri, linkedCts.Token);
                if (status.RuntimeStatus == "Pending" || status.RuntimeStatus == "Running" || status.RuntimeStatus == "ContinuedAsNew")
                {
                    await Task.Delay(_executionConfiguration.PollingIntervalMs, linkedCts.Token);
                }
                else
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                var reason = timeoutCts.IsCancellationRequested ? "StepTimedOut" : "StepWasCanceled";
                await CancelAsync(client, startResponse.TerminatePostUri, reason);
                if (timeoutCts.IsCancellationRequested)
                {
                    _logger.LogWarning("{ExecutionId} {Step} Step execution timed out", Step.ExecutionId, Step);
                    return Result.Failure("Step execution timed out"); // Report failure => allow possible retries
                }
                throw; // Step was canceled => pass the exception => no retries
            }
        }

        if (status.RuntimeStatus == "Completed")
        {
            return Result.Success(status.Output.ToString());
        }
        else if (status.RuntimeStatus == "Terminated")
        {
            return Result.Failure($"Function was terminated: {status.Output}");
        }
        else
        {
            return Result.Failure($"Function failed: {status.Output}");
        }
    }

    private async Task CancelAsync(HttpClient client, string terminateUrl, string reason)
    {
        try
        {
            var url = terminateUrl.Replace("{text}", reason);
            var response = await client.PostAsync(url, null!);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping function ", Step.ExecutionId, Step);
        }
    }

    private async Task<StatusResponse> TryGetStatusAsync(HttpClient client, string statusUrl, CancellationToken cancellationToken)
    {
        int refreshRetries = 0;
        while (refreshRetries < MaxRefreshRetries)
        {
            try
            {
                var response = await client.GetAsync(statusUrl, CancellationToken.None);
                var content = await response.Content.ReadAsStringAsync(CancellationToken.None);
                var statusResponse = JsonSerializer.Deserialize<StatusResponse>(content, JsonSerializerOptions)
                    ?? throw new InvalidOperationException("Status response was null");
                return statusResponse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting function instance status", Step.ExecutionId, Step);
                refreshRetries++;
                await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
            }
        }
        throw new TimeoutException("The maximum number of function instance status refresh attempts was reached.");
    }

    private record StartResponse(string Id, string StatusQueryGetUri, string SendEventPostUri, string TerminatePostUri, string PurgeHistoryDeleteUri);

    private record StatusResponse(string Name, string InstanceId, string RuntimeStatus, JsonElement? Input, JsonElement? CustomStatus,
        JsonElement? Output, DateTime CreatedTime, DateTime LastUpdatedTime);

}
