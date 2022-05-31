using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using IronPython.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal abstract class StepExecutorBase
{
    private readonly ILogger<StepExecutorBase> _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    protected int RetryAttemptCounter { get; set; }
    private StepExecution StepExecution { get; init; }

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
                await UpdateExecutionFailedAsync(failure, true);
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
            await UpdateExecutionFailedAsync(failure, true);
            return false;
        }

        // Inspect execution condition expression here
        try
        {
            var result = EvaluateExecutionCondition();
            if (!result)
            {
                await UpdateExecutionSkipped("Execution condition evaluated as false");
                return false;
            }
        }
        catch (Exception ex)
        {
            var failure = Result.Failure($"Error evaluating execution condition:\n{ex.Message}");
            await UpdateExecutionFailedAsync(failure, true);
            return false;
        }

        // Check whether this step is already running (in another execution). Only include executions from the past 24 hours.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var duplicateExecution = await IsDuplicateExecutionAsync(context);
            // This step execution should be marked as duplicate.
            if (duplicateExecution)
            {
                await UpdateExecutionDuplicateAsync(context);
                _logger.LogWarning("{ExecutionId} {Step} Marked step as DUPLICATE", StepExecution.ExecutionId, StepExecution);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error marking step as DUPLICATE", StepExecution.ExecutionId, StepExecution);
            return false;
        }

        // Loop until there are not retry attempts left.
        while (RetryAttemptCounter <= StepExecution.RetryAttempts)
        {
            await CheckIfStepExecutionIsRetryAttemptAsync();

            // Execute the step based on its step type.
            Result result;
            try
            {
                result = await ExecuteAsync(cancellationTokenSource);
            }
            catch (OperationCanceledException)
            {
                await UpdateExecutionCancelledAsync(cancellationTokenSource.Username);
                return false;
            }
            catch (Exception ex)
            {
                result = Result.Failure("Error during step execution: " + ex.Message);
            }

            if (result is Failure failure)
            {
                _logger.LogWarning("{ExecutionId} {Step} Error executing step: {ErrorMessage}", StepExecution.ExecutionId, StepExecution, failure.ErrorMessage);
                await UpdateExecutionFailedAsync(failure);

                // There are attempts left => increase counter and wait for the retry interval.
                if (RetryAttemptCounter < StepExecution.RetryAttempts)
                {
                    RetryAttemptCounter++;
                    try
                    {
                        await WaitForExecutionRetryAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
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
                await UpdateExecutionSucceededAsync(result);
                return true; // Break the loop to end this execution.
            }
        }

        return false; // Execution should not arrive here in normal conditions. Return false.
    }

    private async Task WaitForExecutionRetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(StepExecution.RetryIntervalMinutes * 60 * 1000, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // If the step was canceled during waiting for a retry, copy a new execution row with STOPPED status.
            _logger.LogWarning("{ExecutionId} {Step} Step was canceled", StepExecution.ExecutionId, StepExecution);
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var prevAttempt = StepExecution.StepExecutionAttempts.OrderByDescending(e => e.RetryAttemptIndex).First();
                var attempt = prevAttempt with
                {
                    RetryAttemptIndex = RetryAttemptCounter,
                    ExecutionStatus = StepExecutionStatus.Stopped,
                    StartDateTime = DateTimeOffset.Now,
                    EndDateTime = DateTimeOffset.Now
                };
                attempt.Reset();
                context.Attach(attempt).State = EntityState.Added;
                await context.SaveChangesAsync(CancellationToken.None);
                StepExecution.StepExecutionAttempts.Add(attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error copying step execution details for retry attempt", StepExecution.ExecutionId, StepExecution);
            }
            throw;
        }
    }

    private async Task UpdateExecutionCancelledAsync(string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == RetryAttemptCounter);
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.StoppedBy = username;
        attempt.ExecutionStatus = StepExecutionStatus.Stopped;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionFailedAsync(Failure failure, bool disregardRetryAttemps = false)
    {
        // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
        var status = RetryAttemptCounter >= StepExecution.RetryAttempts || disregardRetryAttemps
            ? StepExecutionStatus.Failed
            : StepExecutionStatus.AwaitRetry;
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == RetryAttemptCounter);
            attempt.ExecutionStatus = status;
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.ErrorMessage = failure.ErrorMessage;
            attempt.InfoMessage = failure.InfoMessage;
            context.Attach(attempt).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error updating step status to {status}", StepExecution.ExecutionId, StepExecution, status);
        }
    }

    private async Task UpdateExecutionSucceededAsync(Result executionResult)
    {
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == RetryAttemptCounter);
            attempt.ExecutionStatus = StepExecutionStatus.Succeeded;
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.InfoMessage = executionResult.InfoMessage;
            context.Attach(attempt).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error updating step status to SUCCEEDED", StepExecution.ExecutionId, StepExecution);
        }
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

    private async Task CheckIfStepExecutionIsRetryAttemptAsync()
    {
        using var context = _dbContextFactory.CreateDbContext();
        var attempt = StepExecution.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == RetryAttemptCounter);
        if (attempt is not null)
        {
            attempt.StartDateTime = DateTimeOffset.Now;
            attempt.ExecutionStatus = StepExecutionStatus.Running;
            context.Attach(attempt).State = EntityState.Modified;
        }
        else
        {
            var prevAttempt = StepExecution.StepExecutionAttempts.OrderByDescending(e => e.RetryAttemptIndex).First();
            attempt = prevAttempt with
            {
                RetryAttemptIndex = RetryAttemptCounter,
                ExecutionStatus = StepExecutionStatus.Running,
                StartDateTime = DateTimeOffset.Now,
                EndDateTime = null
            };
            attempt.Reset();
            context.Attach(attempt).State = EntityState.Added;
            StepExecution.StepExecutionAttempts.Add(attempt);
        }
        await context.SaveChangesAsync();
    }

    private bool EvaluateExecutionCondition()
    {
        if (!string.IsNullOrWhiteSpace(StepExecution.ExecutionConditionExpression))
        {
            var engine = Python.CreateEngine();
            var scope = engine.CreateScope();
            foreach (var param in StepExecution.ExecutionConditionParameters)
            {
                scope.SetVariable(param.ParameterName, param.ParameterValue);
            }
            return engine.Execute<bool>(StepExecution.ExecutionConditionExpression, scope);
        }
        return true;
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

    private async Task<bool> IsDuplicateExecutionAsync(BiflowContext context)
    {
        var duplicate = await context.StepExecutionAttempts
            .Where(e => e.StepId == StepExecution.StepId && e.ExecutionStatus == StepExecutionStatus.Running && e.StartDateTime >= DateTimeOffset.Now.AddDays(-1))
            .AnyAsync();
        return duplicate;
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
