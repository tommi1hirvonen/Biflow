using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class GlobalOrchestrator : IGlobalOrchestrator
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

    public IEnumerable<Task> RegisterStepExecutionsAsync(
        ICollection<(StepExecution Step, CancellationToken Token)> stepExecutions,
        IOrchestrationListener orchestrationListener)
    {
        List<(StepExecutionStatusObserver Observer, CancellationToken Token)> observers;
        
        // Acquire lock for editing the step statuses and until all observers have subscribed.
        lock (_lock)
        {
            foreach (var (stepExecution, _) in stepExecutions)
            {
                _stepStatuses[stepExecution] = OrchestrationStatus.NotStarted;
            }
            var statuses = _stepStatuses.Select(s => new StepExecutionStatusInfo(s.Key, s.Value)).ToList();
            observers = stepExecutions.Select(x => (new StepExecutionStatusObserver(x.Step, statuses), x.Token)).ToList();
            foreach (var (observer, _) in observers)
            {
                observer.Subscribe(this);
            }
        }

        return observers
            .Select(x => x.Observer.WaitForOrchestrationAsync(orchestrationListener, x.Token))
            .ToList();
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

    public void UpdateStatus(StepExecution step, OrchestrationStatus status)
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

    public async Task QueueAsync(
        StepExecution stepExecution,
        Func<ExtendedCancellationTokenSource, Task> onPreExecute,
        Func<Task> onPostExecute,
        ExtendedCancellationTokenSource cts)
    {
        try
        {
            UpdateStatus(stepExecution, OrchestrationStatus.Running);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating statuses");
        }

        // Update the step's status to Queued.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            foreach (var attempt in stepExecution.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Queued;
                context.Attach(attempt);
                context.Entry(attempt).Property(p => p.ExecutionStatus).IsModified = true;
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {step} Error updating step execution's status to Queued", stepExecution.ExecutionId, stepExecution);
        }

        bool result = false;
        try
        {
            await onPreExecute(cts);

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
            await onPostExecute();
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

}
