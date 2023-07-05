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

    public async Task RegisterStepsAndObservers(List<IOrchestrationObserver> observers)
    {
        try
        {
            List<Task> tasks;
            // Acquire lock for editing the step statuses and until all observers have subscribed.
            lock (_lock)
            {
                foreach (var stepExecution in observers.Select(o => o.StepExecution))
                {
                    _stepStatuses[stepExecution] = OrchestrationStatus.NotStarted;
                }
                var statuses = _stepStatuses.Select(s => new OrchestrationUpdate(s.Key, s.Value)).ToList();
                foreach (var observer in observers)
                {
                    observer.RegisterInitialUpdates(statuses);
                    observer.Subscribe(this);
                }
                tasks = observers.Select(o => o.WaitForProcessingAsync(this)).ToList();
            }
            await Task.WhenAll(tasks);
        }
        finally
        {
            lock (_lock)
            {
                // Clean up any remaining step execution statuses and observers.
                // In normal operation there should be none remaining after orchestration.
                foreach (var observer in observers)
                {
                    if (_stepStatuses.ContainsKey(observer.StepExecution))
                    {
                        UpdateStatus(observer.StepExecution, OrchestrationStatus.Failed);
                        _stepStatuses.Remove(observer.StepExecution);
                    }
                    _observers.Remove(observer);
                }
            }
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

    public Task OnStepReadyForProcessingAsync(
        StepExecution stepExecution,
        StepAction stepAction,
        IStepExecutionListener listener,
        ExtendedCancellationTokenSource cts) =>
        stepAction.Match(
            async (Execute execute) =>
            {
                UpdateStatus(stepExecution, OrchestrationStatus.Running);
                await ExecuteStepAsync(stepExecution, listener, cts);
            },
            async (Cancel cancel) =>
            {
                UpdateStatus(stepExecution, OrchestrationStatus.Failed);
                await UpdateExecutionCancelledAsync(stepExecution, cts.Username);
            },
            async (Fail fail) =>
            {
                UpdateStatus(stepExecution, OrchestrationStatus.Failed);
                await UpdateStepAsync(stepExecution, fail.WithStatus, fail.ErrorMessage);
            });

    private async Task ExecuteStepAsync(StepExecution stepExecution, IStepExecutionListener listener, ExtendedCancellationTokenSource cts)
    {
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
            await listener.OnPreExecuteAsync(cts);

            var executor = _stepExecutorFactory.Create(stepExecution);
            result = await executor.RunAsync(cts);
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

            await listener.OnPostExecuteAsync();
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
                observer.OnUpdate(new(step, status));
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
        if (attempt is null) return;
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = StepExecutionStatus.Failed;
        attempt.StartDateTime ??= DateTimeOffset.Now;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.ErrorMessage = $"Unhandled error caught in global orchestrator:\n\n{ex.Message}\n\n{attempt.ErrorMessage}";
        attempt.ErrorStackTrace = $"{ex.StackTrace}\n\n{attempt.ErrorStackTrace}";
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
