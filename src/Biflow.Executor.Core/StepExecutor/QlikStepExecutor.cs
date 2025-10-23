using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class QlikStepExecutor(
    ILogger<QlikStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IOptionsMonitor<ExecutionOptions> options,
    IHttpClientFactory httpClientFactory,
    QlikStepExecution step,
    QlikStepExecutionAttempt attempt) : IStepExecutor
{
    private readonly QlikCloudClient _client =
        step.GetEnvironment()?.CreateClient(httpClientFactory)
        ?? throw new ArgumentNullException(message: "Qlik environment was null", innerException: null);
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        return step.QlikStepSettings switch
        {
            QlikAppReloadSettings reload => await ReloadAppAsync(reload, cancellationToken),
            QlikAutomationRunSettings run => await RunAutomationAsync(run, cancellationToken),
            _ => throw new ArgumentException($"Unrecognized Qlik step setting type {step.QlikStepSettings.GetType()}")
        };
    }

    private async Task<Result> ReloadAppAsync(QlikAppReloadSettings reloadSettings, CancellationToken cancellationToken)
    {
        QlikAppReload reload;
        try
        {
            reload = await _client.ReloadAppAsync(reloadSettings.AppId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting app reload");
            attempt.AddError(ex, "Error starting app reload");
            return Result.Failure;
        }

        // Create timeout cancellation token source here
        // so that the timeout countdown starts right after the app reload was started.
        using var timeoutCts = step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
                : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Update reload id for the step execution attempt
        try
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.ReloadOrRunId = reload.Id;
            await context.Set<QlikStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId && x.StepId == attempt.StepId && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ReloadOrRunId, attempt.ReloadOrRunId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{ExecutionId} {Step} Error updating app reload id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating app reload id {reload.Id}");
        }

        while (true)
        {
            try
            {
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                reload = await _client.GetReloadAsync(reload.Id, linkedCts.Token);
                switch (reload.Status)
                {
                    case QlikAppReloadStatus.Queued or QlikAppReloadStatus.Reloading:
                        continue;
                    case QlikAppReloadStatus.Succeeded:
                        attempt.AddOutput(reload.Log);
                        return Result.Success;
                    default:
                        attempt.AddOutput(reload.Log);
                        attempt.AddError($"App reload reported status {reload.Status}");
                        return Result.Failure;
                }
            }
            catch (OperationCanceledException ex)
            {
                await CancelReloadAsync(reload.Id);
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
                attempt.AddError(ex, "Error getting app reload status");
                return Result.Failure;
            }
        }
    }

    private async Task<Result> RunAutomationAsync(QlikAutomationRunSettings runSettings,
        CancellationToken cancellationToken)
    {
        QlikAutomationRun run;
        try
        {
            run = await _client.RunAutomationAsync(runSettings.AutomationId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting automation run");
            attempt.AddError(ex, "Error starting automation run");
            return Result.Failure;
        }

        // Create timeout cancellation token source here
        // so that the timeout countdown starts right after the app reload was started.
        using var timeoutCts = step.TimeoutMinutes > 0
                ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
                : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Update reload id for the step execution attempt
        try
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.ReloadOrRunId = run.Id;
            await context.Set<QlikStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId && x.StepId == attempt.StepId && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ReloadOrRunId, attempt.ReloadOrRunId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{ExecutionId} {Step} Error updating automation run id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating automation run id {run.Id}");
        }

        while (true)
        {
            try
            {
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                run = await _client.GetRunAsync(runSettings.AutomationId, run.Id, linkedCts.Token);
                switch (run.Status)
                {
                    case QlikAutomationRunStatus.NotStarted or QlikAutomationRunStatus.Starting or QlikAutomationRunStatus.Running:
                        continue;
                    case QlikAutomationRunStatus.Finished or QlikAutomationRunStatus.FinishedWithWarnings:
                        return Result.Success;
                    default:
                        attempt.AddOutput(run.Error);
                        attempt.AddError($"Automation run reported status {run.Status}");
                        return Result.Failure;
                }
            }
            catch (OperationCanceledException ex)
            {
                await CancelRunAsync(runSettings.AutomationId, run.Id);
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
                attempt.AddError(ex, "Error getting automation run status");
                return Result.Failure;
            }
        }
    }

    private async Task CancelReloadAsync(string reloadId)
    {
        try
        {
            await _client.CancelReloadAsync(reloadId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error canceling app reload", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error canceling app reload");
        }
    }

    private async Task CancelRunAsync(string automationId, string runId)
    {
        try
        {
            await _client.CancelRunAsync(automationId, runId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error canceling automation run", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error canceling automation run");
        }
    }

    public void Dispose()
    {
    }
}
