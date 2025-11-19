using Biflow.Executor.Core.StepExecutor;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class StepOrchestrator(
    ILogger<StepOrchestrator> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IStepExecutorProvider stepExecutorProvider) : IStepOrchestrator
{
    private const int MaxStatusUpdateRetries = 3;
    
    // Define the retry logic for status updates.
    // This logic ensures that at least one attempt is made to update the status.
    // Polly does not elegantly support this approach, so a custom implementation is used.
    private static Task ExecuteWithLateCancellationRetryAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken) =>
        Extensions.ExecuteWithLateCancellationRetryAsync(
            action: action,
            retryCount: MaxStatusUpdateRetries,
            sleepDurationProvider: retryAttempt => Math.Pow(2, retryAttempt) * TimeSpan.FromSeconds(5),
            shouldRetry: ex => ex is not OperationCanceledException,
            cancellationToken);
    
    public async Task<bool> RunAsync(OrchestrationContext context, StepExecution stepExecution,
        CancellationContext cancellationContext)
    {
        // If the step was canceled already before it was even started, update the status to STOPPED.
        if (cancellationContext.IsCancellationRequested)
        {
            await UpdateExecutionStoppedAsync(stepExecution, cancellationContext.Username);
            return false;
        }

        var executionAttempt = stepExecution.StepExecutionAttempts.First();

        // If the step is using job parameters, update the job parameter's current value for this execution.
        // Also, evaluate step execution parameter expressions.
        if (stepExecution is IHasStepExecutionParameters hasParameters)
        {
            try
            {
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
                // Update the historized ExecutionParameterValue with the ExecutionParameter's value.
                foreach (var param in hasParameters.StepExecutionParameters.Where(p => p.InheritFromExecutionParameter is not null))
                {
                    param.ExecutionParameterValue = param.InheritFromExecutionParameter?.ParameterValue ?? param.ExecutionParameterValue;
                    await dbContext.Set<StepExecutionParameterBase>()
                        .Where(x => x.ExecutionId == param.ExecutionId && x.ParameterId == param.ParameterId)
                        .ExecuteUpdateAsync(x => x
                            .SetProperty(p => p.ExecutionParameterValue, param.ExecutionParameterValue), CancellationToken.None);
                }
                // Also, evaluate expressions and save the result to the step parameter's value property.
                foreach (var param in hasParameters.StepExecutionParameters.Where(p => p.UseExpression))
                {
                    param.ParameterValue = new(await param.EvaluateAsync());
                    await dbContext.Set<StepExecutionParameterBase>()
                        .Where(x => x.ExecutionId == param.ExecutionId && x.ParameterId == param.ParameterId)
                        .ExecuteUpdateAsync(x => x
                            .SetProperty(p => p.ParameterValue, param.ParameterValue), CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ExecutionId} {Step} Error updating parameter values", stepExecution.ExecutionId, stepExecution);
                executionAttempt.AddError(ex, "Error updating parameter values");
                await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed,
                    cancellationContext.ShutdownToken);
                return false;
            }
        }

        // Update current values of job parameters to execution condition parameters.
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            foreach (var param in stepExecution.ExecutionConditionParameters.Where(p => p.ExecutionParameter is not null))
            {
                param.ExecutionParameterValue = param.ExecutionParameter?.ParameterValue ?? param.ExecutionParameterValue;
                await dbContext.Set<StepExecutionConditionParameter>()
                    .Where(x => x.ExecutionId == param.ExecutionId && x.ParameterId == param.ParameterId)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(p => p.ExecutionParameterValue, param.ExecutionParameterValue), CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error updating execution parameter values to step execution condition parameters",
                stepExecution.ExecutionId, stepExecution);
            executionAttempt.AddError(ex, "Error updating execution parameter values to inheriting execution condition parameters");
            await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed,
                cancellationContext.ShutdownToken);
            return false;
        }

        // Inspect the execution condition expression here
        try
        {
            var result = await stepExecution.EvaluateExecutionConditionAsync();
            if (!result)
            {
                await UpdateExecutionSkippedAsync(stepExecution, "Execution condition evaluated as false",
                    cancellationContext.ShutdownToken);
                return false;
            }
        }
        catch (Exception ex)
        {
            executionAttempt.AddError(ex, "Error evaluating execution condition");
            await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed,
                cancellationContext.ShutdownToken);
            return false;
        }
        
        return await ExecuteRecursivelyWithRetriesAsync(context, stepExecution, executionAttempt, cancellationContext);
    }

    private async Task<bool> ExecuteRecursivelyWithRetriesAsync(
        OrchestrationContext context,
        StepExecution stepExecution,
        StepExecutionAttempt executionAttempt,
        CancellationContext cancellationContext)
    {
        Result result;
        IStepExecutor? stepExecutor = null;
        try
        {
            await UpdateExecutionRunningAsync(executionAttempt, cancellationContext.ShutdownToken);
            stepExecutor = stepExecutorProvider.GetExecutorFor(stepExecution, executionAttempt);
            result = await stepExecutor.ExecuteAsync(context, cancellationContext);
        }
        catch (OperationCanceledException ex) when (cancellationContext.IsCancellationRequested)
        {
            executionAttempt.AddWarning(ex);
            result = Result.Cancel;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Unhandled error caught in step orchestrator",
                stepExecution.ExecutionId, stepExecution);
            executionAttempt.AddError(ex, "Unhandled error caught in step orchestrator");
            result = Result.Failure;
        }
        finally
        {
            // Some executors may have created disposable resources.
            if (stepExecutor is IDisposable disposable) disposable.Dispose();
        }

        return await result.Match(
            async (Success success) =>
            {
                logger.LogInformation("{ExecutionId} {Step} Step executed successfully", stepExecution.ExecutionId, stepExecution);
                await UpdateExecutionSucceededAsync(executionAttempt, cancellationContext.ShutdownToken);
                return true;
            },
            async (Cancel cancel) =>
            {
                await UpdateExecutionCancelledAsync(executionAttempt, cancellationContext.Username,
                    cancellationContext.ShutdownToken);
                return false;
            },
            async (Failure failure) =>
            {
                logger.LogWarning("{ExecutionId} {Step} Error executing step", stepExecution.ExecutionId, stepExecution);

                // Check whether retry attempts have been exhausted and return false if so.
                if (executionAttempt.RetryAttemptIndex >= stepExecution.RetryAttempts)
                {
                    await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed,
                        cancellationContext.ShutdownToken);
                    return false;
                }

                // There are attempts left => update execution with status Retry,
                await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Retry,
                    cancellationContext.ShutdownToken);

                // Add a new attempt, capture the return value and wait for the retry interval.
                var nextExecution = stepExecution.AddAttempt(StepExecutionStatus.AwaitingRetry);
                await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
                {
                    dbContext.Attach(nextExecution).State = EntityState.Added;
                    await dbContext.SaveChangesAsync();
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(stepExecution.RetryIntervalMinutes),
                        cancellationContext.CancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    executionAttempt.AddWarning(ex);
                    logger.LogWarning("{ExecutionId} {Step} Step was canceled", stepExecution.ExecutionId, stepExecution);
                    await UpdateExecutionCancelledAsync(nextExecution, cancellationContext.Username,
                        cancellationContext.ShutdownToken);
                    return false;
                }

                return await ExecuteRecursivelyWithRetriesAsync(context, stepExecution, nextExecution,
                    cancellationContext);
            });
    }
    
    private async Task UpdateExecutionCancelledAsync(StepExecutionAttempt attempt, string username,
        CancellationToken shutdownToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn = DateTimeOffset.Now;
        attempt.StoppedBy = username;
        attempt.ExecutionStatus = StepExecutionStatus.Stopped;
        // Also account for possibly added messages.
        // Try once without cancellation, and following retries with the shutdown token.
        await ExecuteWithLateCancellationRetryAsync(ct => context.StepExecutionAttempts
            .Where(x => x.ExecutionId == attempt.ExecutionId
                        && x.StepId == attempt.StepId
                        && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.InfoMessages, attempt.InfoMessages)
                .SetProperty(p => p.WarningMessages, attempt.WarningMessages)
                .SetProperty(p => p.ErrorMessages, attempt.ErrorMessages)
                .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                .SetProperty(p => p.StoppedBy, attempt.StoppedBy)
                .SetProperty(p => p.StartedOn, attempt.StartedOn)
                .SetProperty(p => p.EndedOn, attempt.EndedOn), ct), shutdownToken);
    }

    private async Task UpdateExecutionFailedAsync(StepExecutionAttempt attempt, StepExecutionStatus status,
        CancellationToken shutdownToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
        attempt.ExecutionStatus = status;
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn = DateTimeOffset.Now;
        // Also account for possibly added messages.
        await ExecuteWithLateCancellationRetryAsync(ct => context.StepExecutionAttempts
            .Where(x => x.ExecutionId == attempt.ExecutionId
                        && x.StepId == attempt.StepId
                        && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.InfoMessages, attempt.InfoMessages)
                .SetProperty(p => p.WarningMessages, attempt.WarningMessages)
                .SetProperty(p => p.ErrorMessages, attempt.ErrorMessages)
                .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                .SetProperty(p => p.StartedOn, attempt.StartedOn)
                .SetProperty(p => p.EndedOn, attempt.EndedOn), ct),
            shutdownToken);
    }

    private async Task UpdateExecutionSucceededAsync(StepExecutionAttempt attempt, CancellationToken shutdownToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
        attempt.ExecutionStatus = !attempt.WarningMessages.Any()
            ? StepExecutionStatus.Succeeded
            : StepExecutionStatus.Warning;
        attempt.EndedOn = DateTimeOffset.Now;
        // Also account for possibly added messages.
        await ExecuteWithLateCancellationRetryAsync(ct => context.StepExecutionAttempts
            .Where(x => x.ExecutionId == attempt.ExecutionId
                        && x.StepId == attempt.StepId
                        && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.InfoMessages, attempt.InfoMessages)
                .SetProperty(p => p.WarningMessages, attempt.WarningMessages)
                .SetProperty(p => p.ErrorMessages, attempt.ErrorMessages)
                .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                .SetProperty(p => p.EndedOn, attempt.EndedOn), ct),
            shutdownToken);
    }

    private async Task UpdateExecutionRunningAsync(StepExecutionAttempt attempt, CancellationToken shutdownToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
        attempt.StartedOn = DateTimeOffset.Now;
        attempt.ExecutionStatus = StepExecutionStatus.Running;
        await ExecuteWithLateCancellationRetryAsync(ct => context.StepExecutionAttempts
            .Where(x => x.ExecutionId == attempt.ExecutionId
                        && x.StepId == attempt.StepId
                        && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.StartedOn, attempt.StartedOn)
                .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus), ct),
            shutdownToken);
    }

    private async Task UpdateExecutionSkippedAsync(StepExecution stepExecution, string infoMessage,
        CancellationToken shutdownToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Skipped;
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.AddOutput(infoMessage);
            await ExecuteWithLateCancellationRetryAsync(ct => context.StepExecutionAttempts
                .Where(x => x.ExecutionId == stepExecution.ExecutionId
                            && x.StepId == stepExecution.StepId
                            && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                    .SetProperty(p => p.StartedOn, attempt.StartedOn)
                    .SetProperty(p => p.EndedOn, attempt.EndedOn)
                    .SetProperty(p => p.InfoMessages, attempt.InfoMessages), ct),
                shutdownToken);
        }
    }

    private async Task UpdateExecutionStoppedAsync(StepExecution stepExecution, string username)
    {
        // Update without cancellation and retries. This method is called because shutdown was requested.
        // Try to update the step status to stopped.
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var now = DateTimeOffset.Now;
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
            attempt.StartedOn = now;
            attempt.EndedOn = now;
            attempt.StoppedBy = username;
        }
        await context.StepExecutionAttempts
            .Where(x => x.ExecutionId == stepExecution.ExecutionId && x.StepId == stepExecution.StepId)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.ExecutionStatus, StepExecutionStatus.Stopped)
                .SetProperty(p => p.StartedOn, now)
                .SetProperty(p => p.EndedOn, now)
                .SetProperty(p => p.StoppedBy, username));
    }
}
