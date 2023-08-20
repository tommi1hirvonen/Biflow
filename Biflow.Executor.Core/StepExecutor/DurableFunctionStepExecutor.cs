using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Text.Json;

namespace Biflow.Executor.Core.StepExecutor;

internal class DurableFunctionStepExecutor : FunctionStepExecutorBase
{
    private readonly ILogger<DurableFunctionStepExecutor> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly int _pollingIntervalMs;

    private const int MaxRefreshRetries = 3;

    private JsonSerializerOptions JsonSerializerOptions { get; } = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public DurableFunctionStepExecutor(
        ILogger<DurableFunctionStepExecutor> logger,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        IOptionsMonitor<ExecutionOptions> options,
        IHttpClientFactory httpClientFactory,
        FunctionStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _httpClientFactory = httpClientFactory;
        _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var client = _httpClientFactory.CreateClient();

        HttpResponseMessage response;
        string content;
        try
        {
            var request = await BuildFunctionInvokeRequestAsync(cancellationToken);

            // Send the request to the function url. This will start the function, if the request was successful.
            // A durable function will return immediately and run asynchronously.
            response = await client.SendAsync(request, cancellationToken);
            content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            AddOutput(content);
        }
        catch (OperationCanceledException ex)
        {
            return cancellationTokenSource.IsCancellationRequested
                ? new Cancel(ex)
                : new Failure(ex, "Invoking durable function timed out");
        }
        catch (Exception ex)
        {
            return new Failure(ex, "Error sending POST request to invoke function");
        }

        StartResponse startResponse;
        try
        {
            response.EnsureSuccessStatusCode();
            startResponse = JsonSerializer.Deserialize<StartResponse>(content, JsonSerializerOptions)
                ?? throw new InvalidOperationException("Start response was null");
        }
        catch (Exception ex)
        {
            return new Failure(ex, "Error getting start response for durable function");
        }

        // Create timeout cancellation token source here
        // so that the timeout countdown starts right after the function was started.
        using var timeoutCts = Step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
                : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Update instance id for the step execution attempt
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = Step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
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
            AddWarning(ex, $"Error updating function instance id {startResponse.Id}");
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
                await CancelAsync(client, startResponse.TerminatePostUri, reason);
                return timeoutCts.IsCancellationRequested
                    ? new Failure(ex, "Step execution timed out")
                    : new Cancel(ex);
            }
            catch (Exception ex)
            {
                return new Failure(ex, "Error getting function status");
            }
        }
        
        if (status.RuntimeStatus == "Completed")
        {
            AddOutput(status.Output?.ToString());
            return new Success();
        }
        else if (status.RuntimeStatus == "Terminated")
        {
            return new Failure(status.Output?.ToString() ?? "Function was terminated");
        }
        else
        {
            return new Failure(status.Output?.ToString() ?? "Function failed");
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
            AddWarning(ex, "Error stopping function");
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
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting function instance status", Step.ExecutionId, Step));

        return await policy.ExecuteAsync(async (cancellationToken) =>
        {
            var response = await client.GetAsync(statusUrl, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var statusResponse = JsonSerializer.Deserialize<StatusResponse>(content, JsonSerializerOptions)
                ?? throw new InvalidOperationException("Status response was null");
            return statusResponse;
        }, cancellationToken);
    }

    private record StartResponse(string Id, string StatusQueryGetUri, string SendEventPostUri, string TerminatePostUri, string PurgeHistoryDeleteUri);

    private record StatusResponse(string Name, string InstanceId, string RuntimeStatus, JsonElement? Input, JsonElement? CustomStatus,
        JsonElement? Output, DateTime CreatedTime, DateTime LastUpdatedTime);
}
