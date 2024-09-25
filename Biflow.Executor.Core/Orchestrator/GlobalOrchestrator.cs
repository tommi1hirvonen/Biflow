using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class GlobalOrchestrator(
    ILogger<GlobalOrchestrator> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IStepExecutorProvider stepExecutorProvider) : IGlobalOrchestrator
{
    private readonly Lock _lock = new();
    private readonly ILogger<GlobalOrchestrator> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IStepExecutorProvider _stepExecutorProvider = stepExecutorProvider;
    private readonly List<IOrchestrationObserver> _observers = [];
    private readonly Dictionary<StepExecution, OrchestrationStatus> _stepStatuses = [];

    public async Task RegisterStepsAndObserversAsync(IEnumerable<IOrchestrationObserver> observers)
    {
        try
        {
            List<Task> tasks = [];
            List<StepExecutionMonitor> monitors = [];
            // Acquire lock for editing the step statuses and until all observers have subscribed.
            lock (_lock)
            {
                // Update the orchestration statuses with all new observers.
                foreach (var observer in observers)
                {
                    // Existing observers will report back any new monitors caused by the new observers.
                    var monitorsFromExistingObserver = UpdateStatus(observer.StepExecution, OrchestrationStatus.NotStarted);
                    monitors.AddRange(monitorsFromExistingObserver);
                }
                
                // Capture the statuses for all steps currently in orchestration, including the newly added steps.
                var statuses = _stepStatuses.Select(s => new OrchestrationUpdate(s.Key, s.Value)).ToArray();

                // Iterate over the new observers, register initial updates
                // and tell the observers to subscribe for orchestration updates.
                foreach (var observer in observers)
                {
                    // New observers will report back any monitors added because of both previosuly and newly registered step executions.
                    var monitorsFromNewObserver = observer.RegisterInitialUpdates(statuses);
                    monitors.AddRange(monitorsFromNewObserver);
                }

                // After all new observers have subscribed, iterate over them in priority order
                // and check whether the observers request immediate execution or start waiting for processing.
                // In case of immediate execution, the status updates of started steps will be sent to the remaining new observers
                // since they have already subscribed.
                foreach (var observer in observers.OrderBy(o => o.Priority))
                {
                    // Local function to update the orchestration status and to execute the step.
                    // The function is not async, so UpdateStatus() will be called before the Task is returned.
                    Task UpdateAndExecuteAsync(StepExecution stepExecution, IStepExecutionListener listener, ExtendedCancellationTokenSource cts)
                    {
                        // Update the status for observers that have already subscribed.
                        UpdateStatus(stepExecution, OrchestrationStatus.Running);
                        var update = new OrchestrationUpdate(stepExecution, OrchestrationStatus.Running);
                        foreach (var o in observers)
                        {
                            // Push updates to the new observers as initial updates.
                            o.RegisterInitialUpdate(update);
                        }
                        return ExecuteStepAsync(stepExecution, listener, cts);
                    };

                    // Check if the observer is requesting execution immediately before waiting.
                    if (observer.AfterInitialUpdatesRegisteredAsync(executeCallback: UpdateAndExecuteAsync) is Task executeTask)
                    {
                        // If the returned Task is not null, the step can be executed immediately.
                        tasks.Add(executeTask);
                    }
                    else
                    {
                        // Otherwise wait for the observer to be ready for processing.
                        var waitTask = observer.WaitForProcessingAsync(processCallback: OnStepReadyForProcessingAsync);
                        tasks.Add(waitTask);
                    }
                }

                // Tell the observers to subscribe to receive normal updates from the orchestrator
                // after the previous lifecycle methods have been called.
                foreach (var observer in observers)
                {
                    observer.Subscribe(this); // Calling Subscribe() will add the observer to the _observers List.
                }
            }
            _ = AddMonitorsAsync(monitors);
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

    private Task OnStepReadyForProcessingAsync(
        StepExecution stepExecution,
        OrchestratorAction stepAction,
        IStepExecutionListener listener,
        ExtendedCancellationTokenSource cts) =>
        stepAction.Match(
            async (ExecuteAction execute) =>
            {
                UpdateStatus(stepExecution, OrchestrationStatus.Running);
                await ExecuteStepAsync(stepExecution, listener, cts);
            },
            async (CancelAction cancel) =>
            {
                UpdateStatus(stepExecution, OrchestrationStatus.Failed);
                await UpdateExecutionCancelledAsync(stepExecution, cts.Username);
            },
            async (FailAction fail) =>
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
            await listener.OnPreExecuteAsync(stepExecution, cts);
            var stepExecutor = _stepExecutorProvider.GetExecutorFor(stepExecution, stepExecution.StepExecutionAttempts.First());
            result = await stepExecutor.RunAsync(stepExecution, cts);
        }
        catch (OperationCanceledException)
        {
            // We should only arrive here if the step was canceled while it was Queued.
            // If the step was canceled once its execution had started,
            // then the step's executor should handle the cancellation and the result is returned normally from RunAsync().
            try
            {
                await UpdateExecutionCancelledAsync(stepExecution, cts.Username);
            }
            catch { }
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

            await listener.OnPostExecuteAsync(stepExecution);
        }
    }

    private StepExecutionMonitor[] UpdateStatus(StepExecution step, OrchestrationStatus status)
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
            return _observers.ToArray() // Make a copy of the list as observers might unsubscribe during enumeration
                .SelectMany(observer => observer.OnUpdate(new(step, status)))
                .ToArray();
        }
    }

    private async Task UpdateExecutionCancelledAsync(StepExecution stepExecution, string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            context.Attach(attempt);
            attempt.StartedOn ??= DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.StoppedBy = username;
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
        }
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionFailedAsync(Exception ex, StepExecution stepExecution)
    {
        var attempt = stepExecution.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
        if (attempt is null)
        {
            return;
        }
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = StepExecutionStatus.Failed;
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn = DateTimeOffset.Now;
        // Place the error message first on the list.
        attempt.AddError(ex, $"Unhandled error caught in global orchestrator:\n\n{ex.Message}", insertFirst: true);
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

    private async Task UpdateStepAsync(StepExecution step, StepExecutionStatus status, string? errorMessage)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in step.StepExecutionAttempts)
        {
            context.Attach(attempt);
            attempt.ExecutionStatus = status;
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.AddError(errorMessage);
        }
        await context.SaveChangesAsync();
    }

    private async Task AddMonitorsAsync(IEnumerable<StepExecutionMonitor> monitors)
    {
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var distinct = monitors
                .DistinctBy(t => (t.ExecutionId, t.StepId, t.MonitoredExecutionId, t.MonitoredStepId, TrackingReason:t.MonitoringReason));
            context.StepExecutionMonitors.AddRange(distinct);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving new step execution monitors");
        }
    }
}
