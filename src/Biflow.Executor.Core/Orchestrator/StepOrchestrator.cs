using Biflow.Executor.Core.StepExecutor;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class StepOrchestrator(
    ILogger<StepOrchestrator> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IStepExecutorProvider stepExecutorProvider) : IStepOrchestrator
{
    public async Task<bool> RunAsync(OrchestrationContext context, StepExecution stepExecution,
        ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;

        // If the step was canceled already before it was even started, update the status to STOPPED.
        if (cancellationToken.IsCancellationRequested)
        {
            await UpdateExecutionStoppedAsync(stepExecution, cts.Username);
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
                await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
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
            await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
            return false;
        }

        // Inspect the execution condition expression here
        try
        {
            var result = await stepExecution.EvaluateExecutionConditionAsync();
            if (!result)
            {
                await UpdateExecutionSkippedAsync(stepExecution, "Execution condition evaluated as false");
                return false;
            }
        }
        catch (Exception ex)
        {
            executionAttempt.AddError(ex, "Error evaluating execution condition");
            await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
            return false;
        }
        
        return await ExecuteRecursivelyWithRetriesAsync(context, stepExecution, executionAttempt, cts);
    }

    private async Task<bool> ExecuteRecursivelyWithRetriesAsync(
        OrchestrationContext context,
        StepExecution stepExecution,
        StepExecutionAttempt executionAttempt,
        ExtendedCancellationTokenSource cts)
    {
        await UpdateExecutionRunningAsync(executionAttempt);

        Result result;
        try
        {
            using var stepExecutor = stepExecutorProvider.GetExecutorFor(stepExecution, executionAttempt);
            result = await stepExecutor.ExecuteAsync(context, cts);
        }
        catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
        {
            executionAttempt.AddWarning(ex);
            result = Result.Cancel;
        }
        catch (Exception ex)
        {
            executionAttempt.AddError(ex, "Unhandled error caught in step orchestrator");
            result = Result.Failure;
        }

        return await result.Match(
            async (Success success) =>
            {
                logger.LogInformation("{ExecutionId} {Step} Step executed successfully", stepExecution.ExecutionId, stepExecution);
                await UpdateExecutionSucceededAsync(executionAttempt);
                return true;
            },
            async (Cancel cancel) =>
            {
                await UpdateExecutionCancelledAsync(executionAttempt, cts.Username);
                return false;
            },
            async (Failure failure) =>
            {
                logger.LogWarning("{ExecutionId} {Step} Error executing step", stepExecution.ExecutionId, stepExecution);

                // Check whether retry attempts have been exhausted and return false if so.
                if (executionAttempt.RetryAttemptIndex >= stepExecution.RetryAttempts)
                {
                    await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
                    return false;
                }

                // There are attempts left => update execution with status Retry,
                await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Retry);

                // Add a new attempt, capture the return value and wait for the retry interval.
                var nextExecution = stepExecution.AddAttempt(StepExecutionStatus.AwaitingRetry);
                await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
                {
                    dbContext.Attach(nextExecution).State = EntityState.Added;
                    await dbContext.SaveChangesAsync();
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(stepExecution.RetryIntervalMinutes), cts.Token);
                }
                catch (OperationCanceledException ex)
                {
                    executionAttempt.AddWarning(ex);
                    logger.LogWarning("{ExecutionId} {Step} Step was canceled", stepExecution.ExecutionId, stepExecution);
                    await UpdateExecutionCancelledAsync(nextExecution, cts.Username);
                    return false;
                }

                return await ExecuteRecursivelyWithRetriesAsync(context, stepExecution, nextExecution, cts);
            });
    }
    
    private async Task UpdateExecutionCancelledAsync(StepExecutionAttempt attempt, string username)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn = DateTimeOffset.Now;
        attempt.StoppedBy = username;
        attempt.ExecutionStatus = StepExecutionStatus.Stopped;
        // Also account for possibly added messages.
        await context.StepExecutionAttempts
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
                .SetProperty(p => p.EndedOn, attempt.EndedOn));
    }

    private async Task UpdateExecutionFailedAsync(StepExecutionAttempt attempt, StepExecutionStatus status)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        attempt.ExecutionStatus = status;
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn = DateTimeOffset.Now;
        // Also account for possibly added messages.
        await context.StepExecutionAttempts
            .Where(x => x.ExecutionId == attempt.ExecutionId
                        && x.StepId == attempt.StepId
                        && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.InfoMessages, attempt.InfoMessages)
                .SetProperty(p => p.WarningMessages, attempt.WarningMessages)
                .SetProperty(p => p.ErrorMessages, attempt.ErrorMessages)
                .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                .SetProperty(p => p.StartedOn, attempt.StartedOn)
                .SetProperty(p => p.EndedOn, attempt.EndedOn));
    }

    private async Task UpdateExecutionSucceededAsync(StepExecutionAttempt attempt)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        attempt.ExecutionStatus = !attempt.WarningMessages.Any()
            ? StepExecutionStatus.Succeeded
            : StepExecutionStatus.Warning;
        attempt.EndedOn = DateTimeOffset.Now;
        // Also account for possibly added messages.
        await context.StepExecutionAttempts
            .Where(x => x.ExecutionId == attempt.ExecutionId
                        && x.StepId == attempt.StepId
                        && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.InfoMessages, attempt.InfoMessages)
                .SetProperty(p => p.WarningMessages, attempt.WarningMessages)
                .SetProperty(p => p.ErrorMessages, attempt.ErrorMessages)
                .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                .SetProperty(p => p.EndedOn, attempt.EndedOn));
    }

    private async Task UpdateExecutionStoppedAsync(StepExecution stepExecution, string username)
    {
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

    private async Task UpdateExecutionRunningAsync(StepExecutionAttempt attempt)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        attempt.StartedOn = DateTimeOffset.Now;
        attempt.ExecutionStatus = StepExecutionStatus.Running;
        await context.StepExecutionAttempts
            .Where(x => x.ExecutionId == attempt.ExecutionId
                        && x.StepId == attempt.StepId
                        && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
            .ExecuteUpdateAsync(x => x
                .SetProperty(p => p.StartedOn, attempt.StartedOn)
                .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus));
    }

    private async Task UpdateExecutionSkippedAsync(StepExecution stepExecution, string infoMessage)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Skipped;
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.AddOutput(infoMessage);
            await context.StepExecutionAttempts
                .Where(x => x.ExecutionId == stepExecution.ExecutionId
                            && x.StepId == stepExecution.StepId
                            && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                    .SetProperty(p => p.StartedOn, attempt.StartedOn)
                    .SetProperty(p => p.EndedOn, attempt.EndedOn)
                    .SetProperty(p => p.InfoMessages, attempt.InfoMessages));
        }
    }
}
