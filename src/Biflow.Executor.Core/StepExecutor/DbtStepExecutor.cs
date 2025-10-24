using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.StepExecutor;

internal class DbtStepExecutor(
    IServiceProvider serviceProvider,
    DbtStepExecution step,
    DbtStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly ILogger<DbtStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<DbtStepExecutor>>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = serviceProvider
        .GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
    private readonly int _pollingIntervalMs = serviceProvider
        .GetRequiredService<IOptionsMonitor<ExecutionOptions>>()
        .CurrentValue
        .PollingIntervalMs;
    private readonly DbtClient _client = step
        .GetAccount()
        ?.CreateClient(serviceProvider.GetRequiredService<IHttpClientFactory>())
        ?? throw new ArgumentNullException(message: "DbtAccount was null", innerException: null);

    private const int MaxRefreshRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        DbtJobRun run;
        try
        {
            run = await _client.TriggerJobRunAsync(step.DbtJob.Id, cancellationToken);
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
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.DbtJobRunId = run.Id;
            await dbContext.Set<DbtStepExecutionAttempt>()
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
                run = await GetRunWithRetriesAsync(run.Id, linkedCts.Token);
                if (run.Status is DbtJobRunStatus.Success or DbtJobRunStatus.Error or DbtJobRunStatus.Cancelled)
                {
                    break;
                }
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
            }
        }
        catch (OperationCanceledException ex)
        {
            var cancelRun = await CancelAsync(run.Id);
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

    private async Task<DbtJobRun> GetRunWithRetriesAsync(long runId, CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: _ => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, _) =>
                _logger.LogWarning(ex, "{ExecutionId} {Step} Error getting dbt run for id {runId}", step.ExecutionId, step, runId));

        var run = await policy.ExecuteAsync(cancellation =>
            _client.GetJobRunAsync(runId, cancellationToken: cancellation), cancellationToken);
        return run;
    }

    private async Task<DbtJobRun?> CancelAsync(long runId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping dbt run id {runId}", step.ExecutionId, step, runId);
        try
        {
            return await _client.CancelJobRunAsync(runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping dbt run {runId}", step.ExecutionId, step, runId);
            attempt.AddWarning(ex, $"Error stopping dbt run {runId}");
            return null;
        }
    }
}
