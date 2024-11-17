using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Text.Json;

namespace Biflow.Executor.Core.StepExecutor;

internal class DbtStepExecutor(
    ILogger<DbtStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IOptionsMonitor<ExecutionOptions> options,
    IHttpClientFactory httpClientFactory)
    : StepExecutor<DbtStepExecution, DbtStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<DbtStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    private const int MaxRefreshRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    protected override async Task<Result> ExecuteAsync(DbtStepExecution step, DbtStepExecutionAttempt attempt, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var client = step.GetAccount()?.CreateClient(_httpClientFactory);
        ArgumentNullException.ThrowIfNull(client);

        DbtJobRun run;
        try
        {
            run = await client.TriggerJobRunAsync(step.DbtJob.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting dbt job run");
            attempt.AddError(ex, "Error starting dbt job run");
            return Result.Failure;
        }

        // Create timeout cancellation token source here
        // so that the timeout countdown starts right after the app reload was started.
        using var timeoutCts = step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
                : new CancellationTokenSource();
        
        // Update run id for the step execution attempt.
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.DbtJobRunId = run.Id;
            await context.Set<DbtStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId && x.StepId == attempt.StepId && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.DbtJobRunId, attempt.DbtJobRunId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating dbt job run id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating dbt job run id {run.Id}");
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            while (true)
            {
                run = await GetRunWithRetriesAsync(client, step, run.Id, linkedCts.Token);
                if (run.Status is DbtJobRunStatus.Success or DbtJobRunStatus.Error or DbtJobRunStatus.Cancelled)
                {
                    break;
                }
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
            }
        }
        catch (OperationCanceledException ex)
        {
            var cancelRun = await CancelAsync(client, step, attempt, run.Id);
            var cancelJson = JsonSerializer.Serialize(cancelRun ?? run, JsonOptions);
            attempt.AddOutput(cancelJson);
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
            var errorJson = JsonSerializer.Serialize(run, JsonOptions);
            attempt.AddOutput(errorJson);
            attempt.AddError(ex, "Error getting job run status");
            return Result.Failure;
        }

        var json = JsonSerializer.Serialize(run, JsonOptions);
        attempt.AddOutput(json);

        if (run.Status == DbtJobRunStatus.Error)
        {
            attempt.AddError(run.StatusMessage);
        }

        return run.Status == DbtJobRunStatus.Success
            ? Result.Success
            : Result.Failure;
    }

    private async Task<DbtJobRun> GetRunWithRetriesAsync(
        DbtClient client,
        DbtStepExecution step,
        long runId,
        CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: _ => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, _) =>
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting dbt run for id {runId}", step.ExecutionId, step, runId));

        var run = await policy.ExecuteAsync(cancellation =>
            client.GetJobRunAsync(runId, cancellationToken: cancellation), cancellationToken);
        return run;
    }

    private async Task<DbtJobRun?> CancelAsync(
        DbtClient client,
        DbtStepExecution step,
        DbtStepExecutionAttempt attempt,
        long runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping dbt run id {runId}", step.ExecutionId, step, runId);
        try
        {
            return await client.CancelJobRunAsync(runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping dbt run {runId}", step.ExecutionId, step, runId);
            attempt.AddWarning(ex, $"Error stopping dbt run {runId}");
            return null;
        }
    }
}
