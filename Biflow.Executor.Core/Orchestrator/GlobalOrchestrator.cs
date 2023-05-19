using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class GlobalOrchestrator : IGlobalOrchestrator, IStepReadyForProcessingListener
{
    private readonly object _lock = new();
    private readonly ILogger<GlobalOrchestrator> _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    private readonly IStepExecutorFactory _stepExecutorFactory;
    private readonly List<IOrchestrationObserver> _observers = new();
    private readonly Dictionary<StepExecution, OrchestrationStatus> _stepStatuses = new();

    public GlobalOrchestrator(
        ILogger<GlobalOrchestrator> logger,
        IDbContextFactory<BiflowContext> dbContextFactory,
        IStepExecutorFactory stepExecutorFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _stepExecutorFactory = stepExecutorFactory;
    }

    public IEnumerable<Task> RegisterStepsAndObservers(IEnumerable<IOrchestrationObserver> observers)
    {   
        // Acquire lock for editing the step statuses and until all observers have subscribed.
        lock (_lock)
        {
            foreach (var stepExecution in observers.Select(o => o.StepExecution))
            {
                _stepStatuses[stepExecution] = OrchestrationStatus.NotStarted;
            }
            var statuses = _stepStatuses.Select(s => new StepExecutionStatusInfo(s.Key, s.Value)).ToList();
            foreach (var observer in observers)
            {
                observer.RegisterInitialStepExecutionStatuses(statuses);
                observer.Subscribe(this);
            }
            return observers.Select(o => o.WaitForProcessingAsync(this)).ToList();
        }
    }

    public IDisposable Subscribe(IOrchestrationObserver observer)
    {
        lock (_lock)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }
        return new Unsubscriber(_observers, observer);
    }

    public async Task OnStepReadyForProcessingAsync(StepExecution stepExecution, StepAction stepAction, IStepProcessingListener listener, ExtendedCancellationTokenSource cts)
    {
        var context = new StepProcessingContext();

        await listener.OnPreQueuedAsync(context, stepAction);

        if (context.FailStatus is StepExecutionStatus failStatus)
        {
            await UpdateStepAsync(stepExecution, failStatus, context.ErrorMessage);
            UpdateStatus(stepExecution, OrchestrationStatus.Failed);
            return;
        }

        UpdateStatus(stepExecution, OrchestrationStatus.Running);
        
        // Update the step's status to Queued.
        try
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            foreach (var attempt in stepExecution.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Queued;
                dbContext.Attach(attempt);
                dbContext.Entry(attempt).Property(p => p.ExecutionStatus).IsModified = true;
            }
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {step} Error updating step execution's status to Queued", stepExecution.ExecutionId, stepExecution);
        }

        bool result = false;
        try
        {
            await listener.OnPreExecuteAsync(context, cts);

            if (context.FailStatus is StepExecutionStatus failStatus2)
            {

                await UpdateStepAsync(stepExecution, failStatus2, context.ErrorMessage);
                // No need to update orchestrator status, as it is updated in the finally block.
                return;
            }

            // Create a new step worker.
            var executor = _stepExecutorFactory.Create(stepExecution);
            // Execute the worker and capture the result.
            var task = executor.RunAsync(cts);
            result = await task;
        }
        catch (OperationCanceledException)
        {
            // We should only arrive here if the step was canceled while it was Queued.
            // If the step was canceled once its execution had started,
            // then the step's executor should handle the cancellation and the result is returned normally from RunAsync().
            await UpdateExecutionCancelledAsync(stepExecution, cts.Username);
        }
        catch (Exception ex)
        {
            try
            {
                await UpdateExecutionFailedAsync(ex, stepExecution);
            }
            catch { }
        }
        finally
        {
            var status = result ? OrchestrationStatus.Succeeded : OrchestrationStatus.Failed;
            UpdateStatus(stepExecution, status);
            await listener.OnPostExecuteAsync(context);
        }
    }

    private void UpdateStatus(StepExecution step, OrchestrationStatus status)
    {
        lock (_lock)
        {
            if (status == OrchestrationStatus.Succeeded || status == OrchestrationStatus.Failed)
            {
                _stepStatuses.Remove(step);
            }
            else
            {
                _stepStatuses[step] = status;
            }
            foreach (var observer in _observers.ToArray()) // Make a copy of the list as observers might unsubscribe during enumeration
            {
                observer.OnStepExecutionStatusChange(new(step, status));
            }
        }
    }

    private async Task UpdateExecutionCancelledAsync(StepExecution stepExecution, string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            attempt.StartDateTime ??= DateTimeOffset.Now;
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.StoppedBy = username;
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionFailedAsync(Exception ex, StepExecution stepExecution)
    {
        var attempt = stepExecution.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
        if (attempt is null) return; // return is allowed here because the finally block is executed anyway.
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = StepExecutionStatus.Failed;
        attempt.StartDateTime ??= DateTimeOffset.Now;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.ErrorMessage = $"Unhandled error caught in global orchestrator:\n\n{ex.Message}\n\n{ex.StackTrace}\n\n{attempt.ErrorMessage}";
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateStepAsync(StepExecution step, StepExecutionStatus status, string? errorMessage)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in step.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = status;
            attempt.StartDateTime = DateTimeOffset.Now;
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.ErrorMessage = errorMessage;
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

}
