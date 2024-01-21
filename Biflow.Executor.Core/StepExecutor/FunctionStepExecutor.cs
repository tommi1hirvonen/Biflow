using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Text.Json;

namespace Biflow.Executor.Core.StepExecutor;

internal class FunctionStepExecutor(
    ILogger<FunctionStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IOptionsMonitor<ExecutionOptions> options,
    IHttpClientFactory httpClientFactory,
    FunctionStepExecution step) : IStepExecutor<FunctionStepExecutionAttempt>
{
    private readonly ILogger<FunctionStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    private readonly FunctionStepExecution _step = step;
    private readonly FunctionApp _functionApp = step.GetApp()
        ?? throw new ArgumentNullException(nameof(FunctionApp));

    private const int MaxRefreshRetries = 3;

    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public FunctionStepExecutionAttempt Clone(FunctionStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public Task<Result> ExecuteAsync(FunctionStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();
        return _step switch
        {
            { FunctionIsDurable: true } => RunDurableFunctionAsync(attempt, cancellationTokenSource),
            _ => RunHttpFunctionAsync(attempt, cancellationToken)
        };
    }

    private async Task<Result> RunDurableFunctionAsync(FunctionStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var client = _httpClientFactory.CreateClient();

        HttpResponseMessage response;
        string content;
        try
        {
            var request = await BuildFunctionInvokeRequestAsync(attempt, cancellationToken);

            // Send the request to the function url. This will start the function, if the request was successful.
            // A durable function will return immediately and run asynchronously.
            response = await client.SendAsync(request, cancellationToken);
            content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            attempt.AddOutput(content);
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }
            attempt.AddError(ex, "Invoking durable function timed out");
            return Result.Failure;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error sending POST request to invoke function");
            return Result.Failure;
        }

        StartResponse startResponse;
        try
        {
            response.EnsureSuccessStatusCode();
            startResponse = JsonSerializer.Deserialize<StartResponse>(content, CamelCaseOptions)
                ?? throw new InvalidOperationException("Start response was null");
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error getting start response for durable function");
            return Result.Failure;
        }

        // Create timeout cancellation token source here
        // so that the timeout countdown starts right after the function was started.
        using var timeoutCts = _step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
                : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Update instance id for the step execution attempt
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            attempt.FunctionInstanceId = startResponse.Id;
            context.Attach(attempt);
            context.Entry(attempt).Property(e => e.FunctionInstanceId).IsModified = true;
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating function instance id", _step.ExecutionId, _step);
            attempt.AddWarning(ex, $"Error updating function instance id {startResponse.Id}");
        }

        StatusResponse status;
        while (true)
        {
            try
            {
                status = await GetStatusWithRetriesAsync(client, startResponse.StatusQueryGetUri, linkedCts.Token);
                if (status.RuntimeStatus == "Pending" || status.RuntimeStatus == "Running" || status.RuntimeStatus == "ContinuedAsNew")
                {
                    await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                }
                else
                {
                    break;
                }
            }
            catch (OperationCanceledException ex)
            {
                var reason = timeoutCts.IsCancellationRequested ? "StepTimedOut" : "StepWasCanceled";
                await CancelAsync(attempt, client, startResponse.TerminatePostUri, reason);
                if (timeoutCts.IsCancellationRequested)
                {
                    attempt.AddError(ex, "Step execution timed out");
                    return Result.Failure;
                }
                attempt.AddWarning(ex);
                return Result.Cancel;
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "Error getting function status");
                return Result.Failure;
            }
        }

        if (status.RuntimeStatus == "Completed")
        {
            attempt.AddOutput(status.Output?.ToString());
            return Result.Success;
        }
        else if (status.RuntimeStatus == "Terminated")
        {
            attempt.AddError(status.Output?.ToString() ?? "Function was terminated");
            return Result.Failure;
        }
        else
        {
            attempt.AddError(status.Output?.ToString() ?? "Function failed");
            return Result.Failure;
        }
    }

    private async Task<Result> RunHttpFunctionAsync(FunctionStepExecutionAttempt attempt, CancellationToken cancellationToken)
    {
        using var timeoutCts = _step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(_step.TimeoutMinutes))
                    : new CancellationTokenSource();

        HttpResponseMessage response;
        string content;
        try
        {
            // The linked timeout token will cancel if the timeout expires or the step was canceled manually.
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var request = await BuildFunctionInvokeRequestAsync(attempt, cancellationToken);

            // A regular httpTrigger function can run for several minutes. Use an HttpClient with no timeout for httpTrigger functions.
            var noTimeoutClient = _httpClientFactory.CreateClient("notimeout");

            // Send the request to the function url. This will start the function, if the request was successful.
            response = await noTimeoutClient.SendAsync(request, linkedCts.Token);
            content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            attempt.AddOutput(content);
        }
        catch (OperationCanceledException ex)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                attempt.AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error sending POST request to invoke function");
            return Result.Failure;
        }

        try
        {
            response.EnsureSuccessStatusCode();
            return Result.Success;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Function execution failed");
            return Result.Failure;
        }
    }

    private async Task<HttpRequestMessage> BuildFunctionInvokeRequestAsync(FunctionStepExecutionAttempt attempt, CancellationToken cancellationToken)
    {
        string? functionKey = null;
        try
        {
            // Try and get the function key from the actual step if it was defined.
            using var context = _dbContextFactory.CreateDbContext();
            functionKey = await context.FunctionSteps
                .AsNoTracking()
                .Where(step => step.StepId == _step.StepId)
                .Select(step => step.FunctionKey)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error reading FunctionKey from database", _step.ExecutionId, _step);
            attempt.AddWarning(ex, "Error reading function key from database");
        }

        var message = new HttpRequestMessage(HttpMethod.Post, _step.FunctionUrl);

        // Add function security code as a request header. If the function specific code was defined, use that.
        // Otherwise revert to the function app code if it was defined.
        functionKey ??= _functionApp.FunctionAppKey;
        if (!string.IsNullOrEmpty(functionKey))
        {
            message.Headers.Add("x-functions-key", functionKey);
        }

        // If the input for the function was defined, add it to the request content.
        if (!string.IsNullOrEmpty(_step.FunctionInput))
        {
            var parameters = _step.StepExecutionParameters.ToStringDictionary();
            var input = _step.FunctionInput.Replace(parameters);
            message.Content = new StringContent(input);
        }

        return message;
    }

    private async Task CancelAsync(FunctionStepExecutionAttempt attempt, HttpClient client, string terminateUrl, string reason)
    {
        try
        {
            var url = terminateUrl.Replace("{text}", reason);
            var response = await client.PostAsync(url, null!);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping function ", _step.ExecutionId, _step);
            attempt.AddWarning(ex, "Error stopping function");
        }
    }

    private async Task<StatusResponse> GetStatusWithRetriesAsync(HttpClient client, string statusUrl, CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, waitDuration) =>
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting function instance status", _step.ExecutionId, _step));

        return await policy.ExecuteAsync(async (cancellationToken) =>
        {
            var response = await client.GetAsync(statusUrl, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var statusResponse = JsonSerializer.Deserialize<StatusResponse>(content, CamelCaseOptions)
                ?? throw new InvalidOperationException("Status response was null");
            return statusResponse;
        }, cancellationToken);
    }

    private record StartResponse(string Id, string StatusQueryGetUri, string SendEventPostUri, string TerminatePostUri, string PurgeHistoryDeleteUri);

    private record StatusResponse(string Name, string InstanceId, string RuntimeStatus, JsonElement? Input, JsonElement? CustomStatus,
        JsonElement? Output, DateTime CreatedTime, DateTime LastUpdatedTime);
}
