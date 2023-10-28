using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal abstract class StepExecutorBase
{
    private readonly ILogger<StepExecutorBase> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    
    private StepExecution StepExecution { get; }

    private List<Message> ExecutionMessages { get; } = [];

    protected StepExecutorBase(
        ILogger<StepExecutorBase> logger,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        StepExecution stepExecution)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        StepExecution = stepExecution;
    }

    protected abstract Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource);

    protected void AddError(Exception exception) => ExecutionMessages.Add(new Error(exception));

    protected void AddError(Exception? exception, string message) => ExecutionMessages.Add(new Error(exception, message));

    protected void AddError(string? message)
    {
        if (message is not null)
        {
            ExecutionMessages.Add(new Error(message));
        }
    }

    protected void AddWarning(Exception exception) => ExecutionMessages.Add(new Warning(exception));

    protected void AddWarning(Exception? exception, string message) => ExecutionMessages.Add(new Warning(exception, message));

    protected void AddWarning(string? message)
    {
        if (message is not null)
        {
            ExecutionMessages.Add(new Warning(message));
        }
    }

    protected void AddOutput(string? message)
    {
        if (message is not null)
        {
            ExecutionMessages.Add(new Output(message));
        }
    }

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

        // If the step is using job parameters, update the job parameter's current value for this execution.
        // Also evaluate step execution parameter expressions.
        if (StepExecution is IHasStepExecutionParameters hasParameters)
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
                // Also evaluate expressiosn and save the result to the step parameter's value property.
                foreach (var param in hasParameters.StepExecutionParameters.Where(p => p.UseExpression))
                {
                    context.Attach(param);
                    param.ParameterValue = await param.EvaluateAsync();
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error updating parameter values",
                StepExecution.ExecutionId, StepExecution);
                AddError(ex, "Error updating parameter values");
                await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
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
            AddError(ex, "Error updating execution parameter values to inheriting execution condition parameters");
            await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
            return false;
        }

        // Inspect execution condition expression here
        try
        {
            var result = await StepExecution.EvaluateExecutionConditionAsync();
            if (!result)
            {
                await UpdateExecutionSkippedAsync("Execution condition evaluated as false");
                return false;
            }
        }
        catch (Exception ex)
        {
            AddError(ex, "Error evaluating execution condition");
            await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
            return false;
        }

        return await ExecuteRecursivelyWithRetriesAsync(executionAttempt, cancellationTokenSource);
    }

    private async Task<bool> ExecuteRecursivelyWithRetriesAsync(StepExecutionAttempt executionAttempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        ExecutionMessages.Clear(); // Clear messages before every execution attempt.

        await UpdateExecutionRunningAsync(executionAttempt);

        // Execute the step based on its step type.
        Result result;
        try
        {
            result = await ExecuteAsync(cancellationTokenSource);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException canceled && cancellationTokenSource.IsCancellationRequested)
            {
                AddWarning(canceled);
                result = new Cancel();
            }
            else
            {
                AddError(ex, "Unhandled error caught in base executor");
                result = new Failure();
            }
        }

        return await result.Match(
            async (Success success) =>
            {
                _logger.LogInformation("{ExecutionId} {Step} Step executed successfully", StepExecution.ExecutionId, StepExecution);
                await UpdateExecutionSucceededAsync(executionAttempt);
                return true;
            },
            async (Cancel cancel) =>
            {
                await UpdateExecutionCancelledAsync(executionAttempt, cancellationTokenSource.Username);
                return false;
            },
            async (Failure failure) =>
            {
                _logger.LogWarning("{ExecutionId} {Step} Error executing step", StepExecution.ExecutionId, StepExecution);

                // Check whether retry attempts have been exhausted and return false if so.
                if (executionAttempt.RetryAttemptIndex >= StepExecution.RetryAttempts)
                {
                    await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Failed);
                    return false;
                }

                // There are attempts left => update execution with status Retry,
                await UpdateExecutionFailedAsync(executionAttempt, StepExecutionStatus.Retry);

                // Copy the execution attempt, increase counter and wait for the retry interval.
                var nextExecution = executionAttempt.Clone(executionAttempt.RetryAttemptIndex + 1);
                nextExecution.ExecutionStatus = StepExecutionStatus.AwaitingRetry;
                StepExecution.StepExecutionAttempts.Add(nextExecution);
                using (var context = _dbContextFactory.CreateDbContext())
                {
                    context.Attach(nextExecution).State = EntityState.Added;
                    await context.SaveChangesAsync();
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(StepExecution.RetryIntervalMinutes), cancellationTokenSource.Token);
                }
                catch (OperationCanceledException ex)
                {
                    AddWarning(ex);
                    _logger.LogWarning("{ExecutionId} {Step} Step was canceled", StepExecution.ExecutionId, StepExecution);
                    await UpdateExecutionCancelledAsync(nextExecution, cancellationTokenSource.Username);
                    return false;
                }

                return await ExecuteRecursivelyWithRetriesAsync(nextExecution, cancellationTokenSource);
            });
    }

    private IEnumerable<ErrorMessage> ErrorMessages => ExecutionMessages
        .OfType<Error>()
        .Select(e => new ErrorMessage(e.Message, e.Exception?.ToString()));

    private IEnumerable<WarningMessage> WarningMessages => ExecutionMessages
        .OfType<Warning>()
        .Select(w => new WarningMessage(w.Message, w.Exception?.ToString()));

    private IEnumerable<InfoMessage> OutputMessages => ExecutionMessages
        .OfType<Output>()
        .Select(o => new InfoMessage(o.Message));

    private void SetMessages(StepExecutionAttempt attempt)
    {
        foreach (var error in ErrorMessages)
        {
            attempt.ErrorMessages.Add(error);
        }
        foreach (var warning in WarningMessages)
        {
            attempt.WarningMessages.Add(warning);
        }
        foreach (var info in OutputMessages)
        {
            attempt.InfoMessages.Add(info);
        }
    }

    private async Task UpdateExecutionCancelledAsync(StepExecutionAttempt attempt, string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        SetMessages(attempt);
        attempt.StartDateTime ??= DateTimeOffset.Now;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.StoppedBy = username;
        attempt.ExecutionStatus = StepExecutionStatus.Stopped;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionFailedAsync(StepExecutionAttempt attempt, StepExecutionStatus status)
    {
        using var context = _dbContextFactory.CreateDbContext();
        SetMessages(attempt);
        attempt.ExecutionStatus = status;
        attempt.EndDateTime = DateTimeOffset.Now;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionSucceededAsync(StepExecutionAttempt attempt)
    {
        using var context = _dbContextFactory.CreateDbContext();
        SetMessages(attempt);
        var status = attempt.WarningMessages.Count == 0
            ? StepExecutionStatus.Succeeded
            : StepExecutionStatus.Warning;
        
        attempt.ExecutionStatus = status;
        attempt.EndDateTime = DateTimeOffset.Now;
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

    private async Task UpdateExecutionSkippedAsync(string infoMessage)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in StepExecution.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = StepExecutionStatus.Skipped;
            attempt.StartDateTime = DateTimeOffset.Now;
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.InfoMessages.Add(new(infoMessage));
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

}
