using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal abstract class StepExecutorBase
{
    private readonly ILogger<StepExecutorBase> _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    private readonly IExecutionConfiguration _executionConfiguration;
    
    private StepExecution StepExecution { get; }

    private List<Message> ExecutionMessages { get; } = new();

    protected StepExecutorBase(
        ILogger<StepExecutorBase> logger,
        IDbContextFactory<BiflowContext> dbContextFactory,
        IExecutionConfiguration executionConfiguration,
        StepExecution stepExecution)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _executionConfiguration = executionConfiguration;
        StepExecution = stepExecution;
    }

    protected abstract Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource);

    protected void AddWarning(Exception? exception, string message) => ExecutionMessages.Add(new Warning(exception, message));

    protected void AddWarning(string message) => ExecutionMessages.Add(new Warning(message));

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
                var failure = new Failure(ex, "Error updating parameter values");
                await UpdateExecutionFailedAsync(executionAttempt, failure, StepExecutionStatus.Failed);
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
            var failure = new Failure(ex, "Error updating execution parameter values to inheriting execution condition parameters");
            await UpdateExecutionFailedAsync(executionAttempt, failure, StepExecutionStatus.Failed);
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
            var failure =new Failure(ex, "Error evaluating execution condition");
            await UpdateExecutionFailedAsync(executionAttempt, failure, StepExecutionStatus.Failed);
            return false;
        }

        // If the duplicate execution behaviour is "Allow", continue.
        if (StepExecution.DuplicateExecutionBehaviour != DuplicateExecutionBehaviour.Allow)
        {
            try
            {
                // Otherwise check the defined behaviour and whether there are duplicates of this step running in other executions.
                using var context = _dbContextFactory.CreateDbContext();
                var duplicatesExist = await DuplicatesExistAsync(context, cancellationToken);
                if (duplicatesExist && StepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Fail)
                {
                    await UpdateExecutionDuplicateAsync(context);
                    return false;
                }
                else if (duplicatesExist && StepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Wait)
                {
                    await WaitForDuplicateExecutionsAsync(context, cancellationToken);
                }   
            }
            catch (OperationCanceledException ex)
            {
                await UpdateExecutionCancelledAsync(executionAttempt, new Cancel(ex), cancellationTokenSource.Username);
                return false;
            }
            catch (Exception ex)
            {
                var failure = new Failure(ex, "Error awaiting possible external duplicate executions");
                await UpdateExecutionFailedAsync(executionAttempt, failure, StepExecutionStatus.Failed);
                return false;
            }
        }

        // Check whether there are steps depending on this step running in other executions
        // or whether there are steps running in other executions that this step depends on.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            await WaitForRunningDependentExecutionsAsync(context, cancellationToken); // steps that depend on this step
            if (StepExecution.Execution.DependencyMode)
                await WaitForRunningDependenciesAsync(context, cancellationToken); // steps this step depends on
        }
        catch (OperationCanceledException ex)
        {
            await UpdateExecutionCancelledAsync(executionAttempt, new Cancel(ex), cancellationTokenSource.Username);
            return false;
        }
        catch (Exception ex)
        {
            var failure = new Failure(ex, "Error awaiting dependent steps that were running in another execution in dependency mode");
            await UpdateExecutionFailedAsync(executionAttempt, failure, StepExecutionStatus.Failed);
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
            result = (ex, cancellationTokenSource) switch
            {
                (OperationCanceledException canceled, { IsCancellationRequested: true }) => new Cancel(canceled),
                _ => new Failure(ex, "Unhandled error caught in base executor")
            };
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
                await UpdateExecutionCancelledAsync(executionAttempt, cancel, cancellationTokenSource.Username);
                return false;
            },
            async (Failure failure) =>
            {
                _logger.LogWarning("{ExecutionId} {Step} Error executing step: {ErrorMessage}", StepExecution.ExecutionId, StepExecution, failure.ErrorMessage);

                // Check whether retry attempts have been exhausted and return false if so.
                if (executionAttempt.RetryAttemptIndex >= StepExecution.RetryAttempts)
                {
                    await UpdateExecutionFailedAsync(executionAttempt, failure, StepExecutionStatus.Failed);
                    return false;
                }

                // There are attempts left => update execution with status Retry,
                await UpdateExecutionFailedAsync(executionAttempt, failure, StepExecutionStatus.Retry);

                // Copy the execution attempt, increase counter and wait for the retry interval.
                var nextExecution = executionAttempt.Clone();
                nextExecution.RetryAttemptIndex++;
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
                    var cancel = new Cancel(ex);
                    _logger.LogWarning("{ExecutionId} {Step} Step was canceled", StepExecution.ExecutionId, StepExecution);
                    await UpdateExecutionCancelledAsync(nextExecution, cancel, cancellationTokenSource.Username);
                    return false;
                }

                return await ExecuteRecursivelyWithRetriesAsync(nextExecution, cancellationTokenSource);
            });
    }

    /// <summary>
    /// Wait for possible duplicate executions of this step that are running under a different execution.
    /// </summary>
    private async Task WaitForDuplicateExecutionsAsync(BiflowContext context, CancellationToken cancellationToken)
    {
        while (await DuplicatesExistAsync(context, cancellationToken))
        {
            await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
        }
    }

    /// <summary>
    /// Wait for possible steps that are dependent on this step but are already running under a different execution.
    /// </summary>
    private async Task WaitForRunningDependentExecutionsAsync(BiflowContext context, CancellationToken cancellationToken)
    {
        while (await DependentsExistAsync(context, cancellationToken))
        {
            await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
        }
    }

    /// <summary>
    /// Wait for possible steps that this step depends on and are running under a different execution.
    /// </summary>
    private async Task WaitForRunningDependenciesAsync(BiflowContext context, CancellationToken cancellationToken)
    {
        while (await DependenciesExistAsync(context, cancellationToken))
        {
            await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
        }
    }

    /// <summary>
    /// Checks whether there are duplicates of this step running under a different execution.
    /// Only consider statuses "Running" and "AwaitingRetry". Only include executions from the last 24 hours.
    /// </summary>
    private async Task<bool> DuplicatesExistAsync(BiflowContext context, CancellationToken cancellationToken) =>
        await context.StepExecutionAttempts
        .AsNoTrackingWithIdentityResolution()
        .Where(e => e.StepId == StepExecution.StepId)
        .Where(e => e.ExecutionStatus == StepExecutionStatus.Running || e.ExecutionStatus == StepExecutionStatus.AwaitingRetry)
        .Where(e => e.StartDateTime >= DateTimeOffset.Now.AddDays(-1))
        .AnyAsync(cancellationToken);

    /// <summary>
    /// Checks whether there are steps that depend on this step running under a different execution.
    /// </summary>
    private async Task<bool> DependentsExistAsync(BiflowContext context, CancellationToken cancellationToken) =>
        await context.StepExecutionAttempts
        .AsNoTrackingWithIdentityResolution()
        .Where(e => e.ExecutionStatus == StepExecutionStatus.Running || e.ExecutionStatus == StepExecutionStatus.AwaitingRetry)
        .Where(e => e.StepExecution.Execution.DependencyMode)
        .Where(e => e.StepExecution.ExecutionDependencies.Select(d => d.DependantOnStepId).Contains(StepExecution.StepId))
        .AnyAsync(cancellationToken);

    /// <summary>
    /// Checks whether there are steps that this step depends on running under a different execution.
    /// </summary>
    private async Task<bool> DependenciesExistAsync(BiflowContext context, CancellationToken cancellationToken) =>
        await context.StepExecutionAttempts
        .AsNoTrackingWithIdentityResolution()
        .Where(e => e.ExecutionStatus == StepExecutionStatus.Running || e.ExecutionStatus == StepExecutionStatus.AwaitingRetry)
        .Where(e => StepExecution.ExecutionDependencies.Select(d => d.DependantOnStepId).Contains(e.StepId))
        .AnyAsync(cancellationToken);

    private string? GetWarningMessage()
    {
        var warnings = ExecutionMessages
            .OfType<Warning>()
            .Select(w => w.Exception is not null ? $"{w.Message}:\n{w.Exception.Message}" : w.Message);
        var message = string.Join("\n\n", warnings);
        return string.IsNullOrWhiteSpace(message) ? null : message;
    }

    private string? GetOutputMessage()
    {
        var outputs = ExecutionMessages
            .OfType<Output>()
            .Select(o => o.Message);
        var message = string.Join("\n\n", outputs);
        return string.IsNullOrWhiteSpace(message) ? null : message;
    }

    private async Task UpdateExecutionCancelledAsync(StepExecutionAttempt attempt, Cancel cancel, string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        attempt.StartDateTime ??= DateTimeOffset.Now;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.StoppedBy = username;
        attempt.ExecutionStatus = StepExecutionStatus.Stopped;
        attempt.ErrorMessage = cancel.Exception?.Message;
        attempt.ErrorStackTrace = cancel.Exception?.StackTrace;
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionFailedAsync(StepExecutionAttempt attempt, Failure failure, StepExecutionStatus status)
    {
        var errorMessage = failure.Exception switch
        {
            not null => $"{failure.ErrorMessage}:\n{failure.Exception.Message}",
            _ => failure.ErrorMessage
        };
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = status;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.ErrorMessage = errorMessage;
        attempt.ErrorStackTrace = failure.Exception?.StackTrace;
        attempt.WarningMessage = GetWarningMessage();
        attempt.InfoMessage = GetOutputMessage();
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionSucceededAsync(StepExecutionAttempt attempt)
    {
        var warning = GetWarningMessage();
        var status = string.IsNullOrWhiteSpace(warning)
            ? StepExecutionStatus.Succeeded
            : StepExecutionStatus.Warning;
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = status;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.WarningMessage = GetWarningMessage();
        attempt.InfoMessage = GetOutputMessage();
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
