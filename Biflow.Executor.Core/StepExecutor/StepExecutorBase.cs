using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using CodingSeb.ExpressionEvaluator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal abstract class StepExecutorBase
{
    private readonly ILogger<StepExecutorBase> _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    
    private StepExecution StepExecution { get; }

    protected StepExecutorBase(ILogger<StepExecutorBase> logger, IDbContextFactory<BiflowContext> dbContextFactory, StepExecution stepExecution)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        StepExecution = stepExecution;
    }

    protected abstract Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource);

    public async Task<bool> RunAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;

        // If the step was canceled already before it was even started, update the status to STOPPED.
        if (cancellationToken.IsCancellationRequested)
        {
            await UpdateExecutionStoppedAsync(cancellationTokenSource.Username);
            return false;
        }

        var executionAttempt = StepExecution.StepExecutionAttempts.First();

        // If the step execution is parameterized and it's using job parameters,
        // update the job parameter's current value for this execution.
        if (StepExecution is ParameterizedStepExecution parameterized)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                foreach (var param in parameterized.StepExecutionParameters.Where(p => p.ExecutionParameter is not null))
                {
                    context.Attach(param);
                    param.ExecutionParameterValue = param.ExecutionParameter?.ParameterValue;
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error updating execution parameter values to step parameters",
                StepExecution.ExecutionId, StepExecution);
                var failure = Result.Failure("Error updating execution parameter values to inheriting step parameters");
                await UpdateExecutionFailedAsync(executionAttempt, failure, true);
                return false;
            }
        }

        // Update current values of job parameters to execution condition parameters.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            foreach (var param in StepExecution.ExecutionConditionParameters.Where(p => p.ExecutionParameter is not null))
            {
                context.Attach(param);
                param.ExecutionParameterValue = param.ExecutionParameter?.ParameterValue;
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error updating execution parameter values to step execution condition parameters",
                StepExecution.ExecutionId, StepExecution);
            var failure = Result.Failure("Error updating execution parameter values to inheriting execution condition parameters");
            await UpdateExecutionFailedAsync(executionAttempt, failure, true);
            return false;
        }

        // Inspect execution condition expression here
        try
        {
            var result = await EvaluateExecutionConditionAsync();
            if (!result)
            {
                await UpdateExecutionSkipped("Execution condition evaluated as false");
                return false;
            }
        }
        catch (Exception ex)
        {
            var failure = Result.Failure($"Error evaluating execution condition:\n{ex.Message}");
            await UpdateExecutionFailedAsync(executionAttempt, failure, true);
            return false;
        }

        // Check whether this step is already running (in another execution). Only include executions from the past 24 hours.
        using (var context = _dbContextFactory.CreateDbContext())
        {
            var duplicateExecution = await context.StepExecutionAttempts
                .Where(e => e.StepId == StepExecution.StepId && e.ExecutionStatus == StepExecutionStatus.Running && e.StartDateTime >= DateTimeOffset.Now.AddDays(-1))
                .AnyAsync();
            // This step execution should be marked as duplicate.
            if (duplicateExecution)
            {
                await UpdateExecutionDuplicateAsync(context);
                _logger.LogWarning("{ExecutionId} {Step} Marked step as DUPLICATE", StepExecution.ExecutionId, StepExecution);
                return false;
            }
        }

        // Loop until there are no retry attempts left.
        while (executionAttempt.RetryAttemptIndex <= StepExecution.RetryAttempts)
        {
            await UpdateExecutionRunningAsync(executionAttempt);

            // Execute the step based on its step type.
            Result result;
            try
            {
                result = await ExecuteAsync(cancellationTokenSource);
            }
            catch (OperationCanceledException)
            {
                await UpdateExecutionCancelledAsync(executionAttempt, cancellationTokenSource.Username);
                return false;
            }
            catch (Exception ex)
            {
                result = Result.Failure($"Unhandled error caught in base executor:\n\n{ex.Message}\n\n{ex.StackTrace}");
            }

            if (result is Failure failure)
            {
                _logger.LogWarning("{ExecutionId} {Step} Error executing step: {ErrorMessage}", StepExecution.ExecutionId, StepExecution, failure.ErrorMessage);
                await UpdateExecutionFailedAsync(executionAttempt, failure);

                // There are attempts left => increase counter and wait for the retry interval.
                if (executionAttempt.RetryAttemptIndex < StepExecution.RetryAttempts)
                {
                    executionAttempt = await AddAndGetIncrementedExecutionAttemptAsync(executionAttempt);
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(StepExecution.RetryIntervalMinutes), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("{ExecutionId} {Step} Step was canceled", StepExecution.ExecutionId, StepExecution);
                        await UpdateExecutionCancelledAsync(executionAttempt, cancellationTokenSource.Username);
                        return false;
                    }
                }
                // Otherwise break the loop and end this execution.
                else
                {
                    return false;
                }
            }
            else
            {
                _logger.LogInformation("{ExecutionId} {Step} Step executed successfully", StepExecution.ExecutionId, StepExecution);
                // The step execution was successful. Update the execution accordingly.
                await UpdateExecutionSucceededAsync(executionAttempt, result);
                return true; // Break the loop to end this execution.
            }
        }

        return false; // Execution should not arrive here in normal conditions. Return false.
    }

    private async Task UpdateExecutionCancelledAsync(StepExecutionAttempt attempt, string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        attempt.StartDateTime ??= DateTimeOffset.Now;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.StoppedBy = username;
        attempt.ExecutionStatus = StepExecutionStatus.Stopped;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionFailedAsync(StepExecutionAttempt attempt, Failure failure, bool disregardRetryAttemps = false)
    {
        // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
        var status = attempt.RetryAttemptIndex >= StepExecution.RetryAttempts || disregardRetryAttemps
            ? StepExecutionStatus.Failed
            : StepExecutionStatus.AwaitRetry;
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = status;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.ErrorMessage = failure.ErrorMessage;
        attempt.InfoMessage = failure.InfoMessage;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionSucceededAsync(StepExecutionAttempt attempt, Result executionResult)
    {
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = StepExecutionStatus.Succeeded;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.InfoMessage = executionResult.InfoMessage;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionStoppedAsync(string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in StepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
            attempt.StartDateTime = DateTimeOffset.Now;
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.StoppedBy = username;
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionRunningAsync(StepExecutionAttempt attempt)
    {
        using var context = _dbContextFactory.CreateDbContext();
        attempt.StartDateTime = DateTimeOffset.Now;
        attempt.ExecutionStatus = StepExecutionStatus.Running;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task<bool> EvaluateExecutionConditionAsync()
    {
        if (!string.IsNullOrWhiteSpace(StepExecution.ExecutionConditionExpression))
        {
            var evaluator = new ExpressionEvaluator
            {
                OptionScriptEvaluateFunctionActive = false,
                OptionCanDeclareMultiExpressionsLambdaInSimpleExpressionEvaluate = false
            };
            evaluator.Namespaces.Remove("System.IO");
            evaluator.Variables = StepExecution.ExecutionConditionParameters
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue);
            // Evaluate the expression/statement with a separate Task to allow the executor process to continue.
            return await Task.Run(() => evaluator.Evaluate<bool>(StepExecution.ExecutionConditionExpression));
        }
        return true;
    }

    private async Task<StepExecutionAttempt> AddAndGetIncrementedExecutionAttemptAsync(StepExecutionAttempt previousAttempt)
    {
        var executionAttempt = (previousAttempt with
        {
            RetryAttemptIndex = previousAttempt.RetryAttemptIndex + 1,
        }).Reset();
        StepExecution.StepExecutionAttempts.Add(executionAttempt);
        using var context = _dbContextFactory.CreateDbContext();
        context.Attach(executionAttempt).State = EntityState.Added;
        await context.SaveChangesAsync();
        return executionAttempt;
    }

    private async Task UpdateExecutionSkipped(string infoMessage)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in StepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Skipped;
            attempt.StartDateTime = DateTimeOffset.Now;
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.InfoMessage = infoMessage;
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionDuplicateAsync(BiflowContext context)
    {
        foreach (var attempt in StepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Duplicate;
            attempt.StartDateTime = DateTimeOffset.Now;
            attempt.EndDateTime = DateTimeOffset.Now;
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

}
