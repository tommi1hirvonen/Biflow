using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        var client = step.GetClient()?.CreateConnectedClient(_httpClientFactory);
        ArgumentNullException.ThrowIfNull(client);

        // Start app reload.
        QlikAppReload reload;
        try
        {
            reload = await client.ReloadAppAsync(step.AppId, cancellationToken);
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
            using var context = await _dbContextFactory.CreateDbContextAsync();
            attempt.ReloadId = reload.Id;
            context.Attach(attempt);
            context.Entry(attempt).Property(e => e.ReloadId).IsModified = true;
            await context.SaveChangesAsync(CancellationToken.None);
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
                await CancelAsync(client, step, attempt, reload.Id);
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

    private async Task CancelAsync(
        QlikCloudConnectedClient client,
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
}
