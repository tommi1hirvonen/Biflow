using Biflow.Core.Entities;
using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

namespace Biflow.Executor.Core.StepExecutor;

internal class QlikStepExecutor(
    ILogger<QlikStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IOptionsMonitor<ExecutionOptions> options,
    IHttpClientFactory httpClientFactory)
    : StepExecutor<QlikStepExecution, QlikStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<QlikStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly int _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;

    protected override async Task<Result> ExecuteAsync(
        QlikStepExecution step,
        QlikStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var client = step.GetEnvironment()?.CreateClient(_httpClientFactory);
        ArgumentNullException.ThrowIfNull(client);

        return step.QlikStepSettings switch
        {
            QlikAppReloadSettings reload => await ReloadAppAsync(client, step, attempt, reload, cancellationToken),
            QlikAutomationRunSettings run => await RunAutomationAsync(client, step, attempt, run, cancellationToken),
            _ => throw new ArgumentException($"Unrecognized Qlik step setting type {step.QlikStepSettings.GetType()}")
        };
    }

    private async Task<Result> ReloadAppAsync(
        QlikCloudClient client,
        QlikStepExecution step,
        QlikStepExecutionAttempt attempt,
        QlikAppReloadSettings reloadSettings,
        CancellationToken cancellationToken)
    {
        QlikAppReload reload;
        try
        {
            reload = await client.ReloadAppAsync(reloadSettings.AppId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting app refresh");
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
            using var context = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.ReloadId = reload.Id;
            await context.Set<QlikStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId && x.StepId == attempt.StepId && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ReloadId, attempt.ReloadId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating app reload id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating app reload id {reload.Id}");
        }

        while (true)
        {
            try
            {
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                reload = await client.GetReloadAsync(reload.Id, linkedCts.Token);
                if (reload is { Status: QlikAppReloadStatus.Succeeded })
                {
                    attempt.AddOutput(reload.Log);
                    return Result.Success;
                }
                else if (reload is { Status: QlikAppReloadStatus.Failed or QlikAppReloadStatus.Canceled or QlikAppReloadStatus.ExceededLimit })
                {
                    attempt.AddOutput(reload.Log);
                    attempt.AddError($"Reload reported status {reload.Status}");
                    return Result.Failure;
                }
                // Reload not finished => iterate again
            }
            catch (OperationCanceledException ex)
            {
                var reason = timeoutCts.IsCancellationRequested ? "StepTimedOut" : "StepWasCanceled";
                await CancelReloadAsync(client, step, attempt, reload.Id);
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
                attempt.AddError(ex, "Error getting reload status");
                return Result.Failure;
            }
        }
    }

    private async Task<Result> RunAutomationAsync(
        QlikCloudClient client,
        QlikStepExecution step,
        QlikStepExecutionAttempt attempt,
        QlikAutomationRunSettings runSettings,
        CancellationToken cancellationToken)
    {
        QlikAutomationRun run;
        try
        {
            run = await client.RunAutomationAsync(runSettings.AutomationId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting automation run");
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
            using var context = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.ReloadId = run.Id;
            await context.Set<QlikStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId && x.StepId == attempt.StepId && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ReloadId, attempt.ReloadId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{ExecutionId} {Step} Error updating automation run id", step.ExecutionId, step);
            attempt.AddWarning(ex, $"Error updating automation run id {run.Id}");
        }

        while (true)
        {
            try
            {
                await Task.Delay(_pollingIntervalMs, linkedCts.Token);
                run = await client.GetRunAsync(runSettings.AutomationId, run.Id, linkedCts.Token);
                if (run is { Status: QlikAutomationRunStatus.Finished or QlikAutomationRunStatus.FinishedWithWarnings })
                {
                    //attempt.AddOutput(run.Log);
                    return Result.Success;
                }
                else
                {
                    //attempt.AddOutput(run.Log);
                    attempt.AddError($"Reload reported status {run.Status}");
                    return Result.Failure;
                }
                // Run not finished => iterate again
            }
            catch (OperationCanceledException ex)
            {
                var reason = timeoutCts.IsCancellationRequested ? "StepTimedOut" : "StepWasCanceled";
                await CancelRunAsync(client, step, attempt, runSettings.AutomationId, run.Id);
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
                attempt.AddError(ex, "Error getting run status");
                return Result.Failure;
            }
        }
    }

    private async Task CancelReloadAsync(
        QlikCloudClient client,
        QlikStepExecution step,
        QlikStepExecutionAttempt attempt,
        string reloadId)
    {
        try
        {
            await client.CancelReloadAsync(reloadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error canceling reload", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error canceling reload");
        }
    }

    private async Task CancelRunAsync(
        QlikCloudClient client,
        QlikStepExecution step,
        QlikStepExecutionAttempt attempt,
        string automationId,
        string runId)
    {
        try
        {
            await client.CancelRunAsync(automationId, runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error canceling run", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error canceling run");
        }
    }
}
