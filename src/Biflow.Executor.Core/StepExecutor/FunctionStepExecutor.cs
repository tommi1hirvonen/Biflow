using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Text.Json;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class FunctionStepExecutor(
    ILogger<FunctionStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IOptionsMonitor<ExecutionOptions> options,
    IHttpClientFactory httpClientFactory)
    : StepExecutor<FunctionStepExecution, FunctionStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    private const int MaxRefreshRetries = 3;

    /// <summary>
    /// Options for deserializing durable function start response.
    /// Depending on the DurableTask version, the casing of the property names may vary
    /// => set PropertyNameCaseInsensitive = true
    /// </summary>
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task<Result> ExecuteAsync(
        OrchestrationContext context,
        FunctionStepExecution step,
        FunctionStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();
        
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();

        // The linked timeout token will cancel if the timeout expires or the step was canceled manually.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        
        // Use an HttpClient with no timeout.
        // The step timeout setting is used for request timeout via cancellation token. 
        var noTimeoutClient = httpClientFactory.CreateClient("notimeout");

        HttpResponseMessage? response = null;
        try
        {
            string content;
            try
            {
                using var request = await BuildFunctionInvokeRequestAsync(step, attempt, cancellationToken);

                // Send the request to the function url. This will start the function if the request was successful.
                // A durable function will return immediately and run asynchronously.
                response = await noTimeoutClient.SendAsync(request, cancellationToken);
                attempt.AddOutput($"Response status code: {(int)response.StatusCode} {response.StatusCode}");
                content = await response.Content.ReadAsStringAsync(CancellationToken.None);
                if (!string.IsNullOrEmpty(content))
                {
                    attempt.AddOutput($"Response content:\n{content}");
                }
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    attempt.AddWarning(ex);
                    return Result.Cancel;
                }

                attempt.AddError(ex, "Invoking function timed out");
                return Result.Failure;
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "Error building/sending POST request to invoke function");
                return Result.Failure;
            }

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "HTTP response reported error status code");
                return Result.Failure;
            }

            if (step.DisableAsyncPattern)
            {
                return Result.Success;
            }

            try
            {
                return await PollAsyncPatternAsync(
                    step: step,
                    attempt: attempt,
                    client: noTimeoutClient,
                    initialResponse: response,
                    initialContent: content,
                    isTimeout: () => timeoutCts.IsCancellationRequested,
                    cancellationToken: linkedCts.Token);
            }
            catch (Exception ex)
            {
                attempt.AddError(ex, "Error polling for async pattern");
                return Result.Failure;
            }
        }
        finally
        {
            response?.Dispose();
        }
    }

    private async Task<Result> PollAsyncPatternAsync(
        FunctionStepExecution step,
        FunctionStepExecutionAttempt attempt,
        HttpClient client,
        HttpResponseMessage initialResponse,
        string initialContent,
        Func<bool> isTimeout,
        CancellationToken cancellationToken)
    {
        if (initialResponse.StatusCode != HttpStatusCode.Accepted)
        {
            return Result.Success;
        }
        
        var startResponse = JsonSerializer.Deserialize<StartResponse>(initialContent, CaseInsensitiveOptions);
        if (startResponse is not null)
        {
            return await PollUsingStatusResponseAsync(
                step, attempt, client, startResponse, isTimeout, cancellationToken);
        }
        
        attempt.AddOutput("No status response object was returned from the function. "
                          + "Polling for status using status codes instead.");

        if (initialResponse.Headers.Location is { AbsoluteUri: { Length: > 0 } url })
        {
            return await PollUsingStatusCodesAsync(attempt, client, url, cancellationToken);
        }
        
        attempt.AddWarning("No status response object or location header was returned from the function. "
                           + "Skipped polling.");
        return Result.Success;
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
            logger.LogError(ex, "{ExecutionId} {Step} Error reading FunctionKey from database", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error reading function key from database");
        }

        var message = new HttpRequestMessage(HttpMethod.Post, step.FunctionUrl);

        // Add function security code as a request header. If the function specific code was defined, use that.
        // Otherwise, revert to the function app code if it was defined.
        var functionApp = step.GetApp();

        functionKey ??= functionApp?.FunctionAppKey;
        
        ArgumentException.ThrowIfNullOrEmpty(functionKey);

        message.Headers.Add("x-functions-key", functionKey);
        
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

    private async Task<Result> PollUsingStatusResponseAsync(
        FunctionStepExecution step,
        FunctionStepExecutionAttempt attempt,
        HttpClient client,
        StartResponse startResponse,
        Func<bool> isTimeout,
        CancellationToken cancellationToken)
    {
        // Update instance id for the step execution attempt
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.FunctionInstanceId = startResponse.Id;
            await context.Set<FunctionStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.FunctionInstanceId, attempt.FunctionInstanceId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{ExecutionId} {Step} Error updating function instance id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating function instance id {startResponse.Id}");
        }

        StatusResponse status;
        while (true)
        {
            try
            {
                status = await GetStatusResponseWithRetriesAsync(
                    step,
                    client,
                    startResponse.StatusQueryGetUri,
                    cancellationToken);
                if (status.RuntimeStatus is "Pending" or "Running" or "ContinuedAsNew")
                {
                    await Task.Delay(_pollingIntervalMs, cancellationToken);
                }
                else
                {
                    break;
                }
            }
            catch (OperationCanceledException ex)
            {
                var reason = isTimeout() ? "StepTimedOut" : "StepWasCanceled";
                await CancelAsync(step, attempt, client, startResponse.TerminatePostUri, reason);
                if (isTimeout())
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

    private async Task<StatusResponse> GetStatusResponseWithRetriesAsync(
        FunctionStepExecution step,
        HttpClient client,
        string statusUrl,
        CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: _ => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, _) =>
                logger.LogWarning(ex, "{ExecutionId} {Step} Error getting function instance status", step.ExecutionId, step));

        return await policy.ExecuteAsync(async cancellation =>
        {
            var response = await client.GetAsync(statusUrl, cancellation);
            var content = await response.Content.ReadAsStringAsync(cancellation);
            var statusResponse = JsonSerializer.Deserialize<StatusResponse>(content, CaseInsensitiveOptions)
                ?? throw new InvalidOperationException("Status response was null");
            return statusResponse;
        }, cancellationToken);
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
            logger.LogError(ex, "{ExecutionId} {Step} Error stopping function ", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error stopping function");
        }
    }
    
    private async Task<Result> PollUsingStatusCodesAsync(
        FunctionStepExecutionAttempt attempt,
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        attempt.AddOutput($"Polling URL:\n{url}");
        using var response = await PollAndGetResponseAsync(client, url, cancellationToken);
        
        attempt.AddOutput($"Final polling response status code: {(int)response.StatusCode} {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrEmpty(content))
        {
            attempt.AddOutput($"Final polling response content:\n{content}");
        }

        if (response.IsSuccessStatusCode)
        {
            return Result.Success;
        }
        
        attempt.AddError("Polling response reported error status code");
        return Result.Failure;
    }
    
    private async Task<HttpResponseMessage> PollAndGetResponseAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            await Task.Delay(_pollingIntervalMs, cancellationToken);
            var response = await client.GetAsync(url, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Accepted)
                return response;
            response.Dispose();
        }
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
