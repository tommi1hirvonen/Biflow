using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class StepOrchestrator<TStep, TAttempt, TExecutor>(
    ILogger<StepOrchestrator<TStep, TAttempt, TExecutor>> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IServiceProvider serviceProvider) : IStepOrchestrator
    where TStep : StepExecution
    where TAttempt : StepExecutionAttempt
    where TExecutor : IStepExecutor<TAttempt>
{
    private readonly ILogger<StepOrchestrator<TStep, TAttempt, TExecutor>> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task<bool> RunAsync(StepExecution stepExecution, ExtendedCancellationTokenSource cts)
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
        // Also evaluate step execution parameter expressions.
        if (stepExecution is IHasStepExecutionParameters hasParameters)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                // Update the historized ExecutionParameterValue with the ExecutionParameter's value.
                foreach (var param in hasParameters.StepExecutionParameters.Where(p => p.InheritFromExecutionParameter is not null))
                {
                    context.Attach(param);
                    param.ExecutionParameterValue = param.InheritFromExecutionParameter?.ParameterValue;
                }
                // Also evaluate expressions and save the result to the step parameter's value property.
                foreach (var param in hasParameters.StepExecutionParameters.Where(p => p.UseExpression))
                {
                    context.Attach(param);
                    param.ParameterValue = await param.EvaluateAsync();
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error updating parameter values", stepExecution.ExecutionId, stepExecution);
                executionAttempt.AddError(ex, "Error updating parameter values");
                await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
                return false;
            }
        }

        // Update current values of job parameters to execution condition parameters.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            foreach (var param in stepExecution.ExecutionConditionParameters.Where(p => p.ExecutionParameter is not null))
            {
                context.Attach(param);
                param.ExecutionParameterValue = param.ExecutionParameter?.ParameterValue;
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error updating execution parameter values to step execution condition parameters",
                stepExecution.ExecutionId, stepExecution);
            executionAttempt.AddError(ex, "Error updating execution parameter values to inheriting execution condition parameters");
            await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
            return false;
        }

        // Inspect execution condition expression here
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

        if (stepExecution is TStep step && executionAttempt is TAttempt attempt)
        {
            var executor = ActivatorUtilities.CreateInstance<TExecutor>(_serviceProvider, step);
            return await ExecuteRecursivelyWithRetriesAsync(executor, step, attempt, cts);
        }

        throw new InvalidOperationException($"No matching step executor found for types {stepExecution.GetType()} and {executionAttempt.GetType()}");
    }

    private async Task<bool> ExecuteRecursivelyWithRetriesAsync(
        IStepExecutor<TAttempt> stepExecutor,
        TStep stepExecution,
        TAttempt executionAttempt,
        ExtendedCancellationTokenSource cts)
    {
        await UpdateExecutionRunningAsync(executionAttempt);

        Result result;
        try
        {
            result = await stepExecutor.ExecuteAsync(executionAttempt, cts);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException canceled && cts.IsCancellationRequested)
            {
                executionAttempt.AddWarning(canceled);
                result = Result.Cancel;
            }
            else
            {
                executionAttempt.AddError(ex, "Unhandled error caught in base executor");
                result = Result.Failure;
            }
        }

        return await result.Match(
            async (Success success) =>
            {
                _logger.LogInformation("{ExecutionId} {Step} Step executed successfully", stepExecution.ExecutionId, stepExecution);
                await UpdateExecutionSucceededAsync(executionAttempt);
                return true;
            },
            async (StepExecutor.Cancel cancel) =>
            {
                await UpdateExecutionCancelledAsync(executionAttempt, cts.Username);
                return false;
            },
            async (Failure failure) =>
            {
                _logger.LogWarning("{ExecutionId} {Step} Error executing step", stepExecution.ExecutionId, stepExecution);

                // Check whether retry attempts have been exhausted and return false if so.
                if (executionAttempt.RetryAttemptIndex >= stepExecution.RetryAttempts)
                {
                    await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
                    return false;
                }

                // There are attempts left => update execution with status Retry,
                await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Retry);

                // Copy the execution attempt, increase counter and wait for the retry interval.
                var nextExecution = stepExecutor.Clone(executionAttempt, executionAttempt.RetryAttemptIndex + 1);
                nextExecution.ExecutionStatus = StepExecutionStatus.AwaitingRetry;
                stepExecution.StepExecutionAttempts.Add(nextExecution);
                using (var context = _dbContextFactory.CreateDbContext())
                {
                    context.Attach(nextExecution).State = EntityState.Added;
                    await context.SaveChangesAsync();
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(stepExecution.RetryIntervalMinutes), cts.Token);
                }
                catch (OperationCanceledException ex)
                {
                    executionAttempt.AddWarning(ex);
                    _logger.LogWarning("{ExecutionId} {Step} Step was canceled", stepExecution.ExecutionId, stepExecution);
                    await UpdateExecutionCancelledAsync(nextExecution, cts.Username);
                    return false;
                }

                return await ExecuteRecursivelyWithRetriesAsync(stepExecutor, stepExecution, nextExecution, cts);
            });
    }

    private async Task UpdateExecutionCancelledAsync(StepExecutionAttempt attempt, string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn = DateTimeOffset.Now;
        attempt.StoppedBy = username;
        attempt.ExecutionStatus = StepExecutionStatus.Stopped;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionFailedAsync(StepExecutionAttempt attempt, StepExecutionStatus status)
    {
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = status;
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn = DateTimeOffset.Now;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionSucceededAsync(StepExecutionAttempt attempt)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var status = attempt.WarningMessages.Count == 0
            ? StepExecutionStatus.Succeeded
            : StepExecutionStatus.Warning;

        attempt.ExecutionStatus = status;
        attempt.EndedOn = DateTimeOffset.Now;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionStoppedAsync(StepExecution stepExecution, string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.StoppedBy = username;
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionRunningAsync(StepExecutionAttempt attempt)
    {
        using var context = _dbContextFactory.CreateDbContext();
        attempt.StartedOn = DateTimeOffset.Now;
        attempt.ExecutionStatus = StepExecutionStatus.Running;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionSkippedAsync(StepExecution stepExecution, string infoMessage)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Skipped;
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.InfoMessages.Add(new(infoMessage));
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }
}
