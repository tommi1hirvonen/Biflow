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
    IHttpClientFactory httpClientFactory)
    : StepExecutor<FunctionStepExecution, FunctionStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<FunctionStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    private const int MaxRefreshRetries = 3;

    private static readonly JsonSerializerOptions CamelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override Task<Result> ExecuteAsync(
        FunctionStepExecution step,
        FunctionStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();
        return step switch
        {
            { FunctionIsDurable: true } => RunDurableFunctionAsync(step, attempt, cancellationTokenSource),
            _ => RunHttpFunctionAsync(step, attempt, cancellationToken)
        };
    }

    private async Task<Result> RunDurableFunctionAsync(
        FunctionStepExecution step,
        FunctionStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        var client = _httpClientFactory.CreateClient();

        HttpResponseMessage response;
        string content;
        try
        {
            var request = await BuildFunctionInvokeRequestAsync(step, attempt, cancellationToken);

            // Send the request to the function url. This will start the function, if the request was successful.
            // A durable function will return immediately and run asynchronously.
            response = await client.SendAsync(request, cancellationToken);
            content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            if (!string.IsNullOrEmpty(content))
            {
                attempt.AddOutput($"Response:\n{content}");
            }
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
        using var timeoutCts = step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
                : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Update instance id for the step execution attempt
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.FunctionInstanceId = startResponse.Id;
            await context.Set<FunctionStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId && x.StepId == attempt.StepId && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.FunctionInstanceId, attempt.FunctionInstanceId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating function instance id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating function instance id {startResponse.Id}");
        }

        StatusResponse status;
        while (true)
        {
            try
            {
                status = await GetStatusWithRetriesAsync(client, step, startResponse.StatusQueryGetUri, linkedCts.Token);
                if (status.RuntimeStatus is "Pending" or "Running" or "ContinuedAsNew")
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
                await CancelAsync(step, attempt, client, startResponse.TerminatePostUri, reason);
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

        switch (status.RuntimeStatus)
        {
            case "Completed":
                var output = status.Output?.ToString();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    attempt.AddOutput($"Output:\n{output}");
                }
                return Result.Success;
            case "Terminated":
                attempt.AddError(status.Output?.ToString() ?? "Function was terminated");
                return Result.Failure;
            default:
                attempt.AddError(status.Output?.ToString() ?? "Function failed");
                return Result.Failure;
        }
    }

    private async Task<Result> RunHttpFunctionAsync(
        FunctionStepExecution step,
        FunctionStepExecutionAttempt attempt,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = step.TimeoutMinutes > 0
                    ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
                    : new CancellationTokenSource();

        HttpResponseMessage response;
        try
        {
            // The linked timeout token will cancel if the timeout expires or the step was canceled manually.
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var request = await BuildFunctionInvokeRequestAsync(step, attempt, cancellationToken);

            // A regular httpTrigger function can run for several minutes. Use an HttpClient with no timeout for httpTrigger functions.
            var noTimeoutClient = _httpClientFactory.CreateClient("notimeout");

            // Send the request to the function url. This will start the function, if the request was successful.
            response = await noTimeoutClient.SendAsync(request, linkedCts.Token);
            var content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            if (!string.IsNullOrEmpty(content))
            {
                attempt.AddOutput($"Response:\n{content}");
            }
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

    private async Task<HttpRequestMessage> BuildFunctionInvokeRequestAsync(
        FunctionStepExecution step,
        FunctionStepExecutionAttempt attempt,
        CancellationToken cancellationToken)
    {
        string? functionKey = null;
        try
        {
            // Try and get the function key from the actual step if it was defined.
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            functionKey = await context.FunctionSteps
                .AsNoTracking()
                .Where(s => s.StepId == step.StepId)
                .Select(s => s.FunctionKey)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error reading FunctionKey from database", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error reading function key from database");
        }

        var message = new HttpRequestMessage(HttpMethod.Post, step.FunctionUrl);

        // Add function security code as a request header. If the function specific code was defined, use that.
        // Otherwise, revert to the function app code if it was defined.
        var functionApp = step.GetApp();
        ArgumentNullException.ThrowIfNull(functionApp);

        functionKey ??= functionApp.FunctionAppKey;
        if (!string.IsNullOrEmpty(functionKey))
        {
            message.Headers.Add("x-functions-key", functionKey);
        }
        
        if (string.IsNullOrEmpty(step.FunctionInput))
        {
            return message;
        }
        
        // If the input for the function was defined, add it to the request content.
        var parameters = step.StepExecutionParameters.ToStringDictionary();
        var input = step.FunctionInput.Replace(parameters);
        attempt.AddOutput($"Evaluated function input:\n{input}");
        message.Content = new StringContent(input);

        return message;
    }

    private async Task CancelAsync(
        FunctionStepExecution step, 
        FunctionStepExecutionAttempt attempt,
        HttpClient client,
        string terminateUrl,
        string reason)
    {
        try
        {
            var url = terminateUrl.Replace("{text}", reason);
            var response = await client.PostAsync(url, null!);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping function ", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error stopping function");
        }
    }

    private async Task<StatusResponse> GetStatusWithRetriesAsync(
        HttpClient client,
        FunctionStepExecution step,
        string statusUrl,
        CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: _ => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, _) =>
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting function instance status", step.ExecutionId, step));

        return await policy.ExecuteAsync(async cancellation =>
        {
            var response = await client.GetAsync(statusUrl, cancellation);
            var content = await response.Content.ReadAsStringAsync(cancellation);
            var statusResponse = JsonSerializer.Deserialize<StatusResponse>(content, CamelCaseOptions)
                ?? throw new InvalidOperationException("Status response was null");
            return statusResponse;
        }, cancellationToken);
    }

    private record StartResponse(
        string Id,
        string StatusQueryGetUri,
        string SendEventPostUri,
        string TerminatePostUri,
        string PurgeHistoryDeleteUri);

    private record StatusResponse(string Name,
        string InstanceId,
        string RuntimeStatus,
        JsonElement? Input,
        JsonElement? CustomStatus,
        JsonElement? Output,
        DateTime CreatedTime,
        DateTime LastUpdatedTime);
}
