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
    private readonly Dictionary<Guid, List<Guid>> _childExecutions = [];

    public async Task RegisterStepsAndObserversAsync(
        OrchestrationContext context,
        ICollection<IOrchestrationObserver> observers,
        IStepExecutionListener stepExecutionListener)
    {
        try
        {
            List<Task> tasks = [];
            List<StepExecutionMonitor> monitors = [];
            // Acquire lock for editing the step statuses and until all observers have subscribed.
            lock (_lock)
            {
                // If this is a synchronized child execution, add it to the parent execution's list.
                // The parent is the top level parent execution.
                if (context is { ParentExecutionId: { } parentExecutionId, SynchronizedExecution: true })
                {
                    if (!_childExecutions.TryGetValue(parentExecutionId, out var value))
                    {
                        value = [];
                        _childExecutions[parentExecutionId] = value;
                    }
                    value.Add(context.ExecutionId);
                }
                
                // Update the orchestration statuses with all new observers.
                foreach (var observer in observers)
                {
                    _stepStatuses[observer.StepExecution] = OrchestrationStatus.NotStarted;
                    var update = new OrchestrationUpdate(observer.StepExecution, OrchestrationStatus.NotStarted);

                    // Existing observers will report back any new monitors caused by the new observers.
                    var monitorsFromExistingObservers = _observers
                        .ToArray() // Make a copy of the list as observers might unsubscribe during enumeration.
                        .SelectMany(existingObserver => existingObserver.OnIncomingStepExecutionUpdate(update))
                        .ToArray();
                    monitors.AddRange(monitorsFromExistingObservers);
                }

                // Iterate over the new observers in priority order,
                // register initial updates and capture the generated monitors.
                foreach (var observer in observers.OrderBy(o => o.Priority))
                {
                    // Recollect statuses as new steps can be started during iteration.
                    var statuses = _stepStatuses
                        .Select(s => new OrchestrationUpdate(s.Key, s.Value))
                        .ToArray();

                    // New observers will report back any monitors added because of
                    // both previously registered step executions and because of step executions
                    // from the same execution that may have already been started.
                    var monitorsFromNewObserver = observer.RegisterInitialUpdates(
                        updates: statuses,
                        executeCallback: cts =>
                        {
                            // In case the observer immediately requests for execution,
                            // update the status for observers that have already subscribed and start the execution.
                            UpdateStatus(observer.StepExecution, OrchestrationStatus.Running);
                            var task = ExecuteStepAsync(context, observer.StepExecution, stepExecutionListener, cts);
                            tasks.Add(task);
                        });

                    monitors.AddRange(monitorsFromNewObserver);

                    // If the step was started via the callback when registering initial updates, continue iteration.
                    if (_stepStatuses[observer.StepExecution] != OrchestrationStatus.NotStarted)
                    {
                        continue;
                    }
                    
                    // If the step was not started by the execute callback when registering initial updates,
                    // create a task to wait until the observer requests processing.
                    var waitTask = observer.WaitForProcessingAsync(processCallback: (stepAction, cts) =>
                        OnStepReadyForProcessingAsync(
                            context, observer.StepExecution, stepAction, stepExecutionListener, cts));
                    tasks.Add(waitTask);
                }

                // Tell the observers to subscribe to receive normal updates from the orchestrator
                // after all the previous observer lifecycle methods have been called.
                foreach (var observer in observers)
                {
                    observer.Subscribe(this); // Calling Subscribe() will effectively add the observer to the _observers List.
                }
            }
            _ = AddMonitorsAsync(monitors);
            await Task.WhenAll(tasks);
        }
        finally
        {
            lock (_lock)
            {
                // Remove step execution statuses and observers.
                foreach (var observer in observers)
                {
                    // If this execution is not a child execution or synchronized execution was disabled,
                    // clean up statuses.
                    // Otherwise, this is a synchronized child execution, so do not remove statuses.
                    // In that case, status cleanup is handled by the top level parent.
                    if (context.ParentExecutionId is null || !context.SynchronizedExecution)
                    {
                        _ = _stepStatuses.Remove(observer.StepExecution);
                    }
                    
                    // In normal operation, the observer should already have been removed by unsubscribing.
                    _ = _observers.Remove(observer);
                }

                // If this is a parent execution, check for potential child executions and clear their statuses too.
                if (_childExecutions.TryGetValue(context.ExecutionId, out var childExecutionIds))
                {
                    var keysToRemove = childExecutionIds
                        .Select(id => _stepStatuses.Keys.Where(x => x.ExecutionId == id))
                        .SelectMany(keys => keys)
                        .Distinct()
                        .ToArray();
                    foreach (var key in keysToRemove)
                    {
                        _stepStatuses.Remove(key);
                    }
                    _childExecutions.Remove(context.ExecutionId);
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
            return new Unsubscriber(_observers, observer);
        }
    }

    private Task OnStepReadyForProcessingAsync(
        OrchestrationContext context,
        StepExecution stepExecution,
        OrchestratorAction stepAction,
        IStepExecutionListener listener,
        ExtendedCancellationTokenSource cts) =>
        stepAction.Match(
            async (ExecuteAction _) =>
            {
                UpdateStatus(stepExecution, OrchestrationStatus.Running);
                await ExecuteStepAsync(context, stepExecution, listener, cts);
            },
            async (CancelAction _) =>
            {
                UpdateStatus(stepExecution, OrchestrationStatus.Failed);
                await UpdateExecutionCancelledAsync(stepExecution, cts.Username);
            },
            async (FailAction fail) =>
            {
                UpdateStatus(stepExecution, OrchestrationStatus.Failed);
                await UpdateStepAsync(stepExecution, fail.WithStatus, fail.ErrorMessage);
            });

    private async Task ExecuteStepAsync(OrchestrationContext context, StepExecution stepExecution,
        IStepExecutionListener listener, ExtendedCancellationTokenSource cts)
    {
        // Update the step's status to 'Queued'.
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            foreach (var attempt in stepExecution.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Queued;
                await dbContext.StepExecutionAttempts
                    .Where(x => x.ExecutionId == attempt.ExecutionId &&
                                x.StepId == attempt.StepId &&
                                x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus), CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {step} Error updating step execution's status to Queued",
                stepExecution.ExecutionId, stepExecution);
        }

        var result = false;
        try
        {
            await listener.OnPreExecuteAsync(stepExecution, cts);
            var stepExecutor = _stepExecutorProvider
                .GetExecutorFor(stepExecution, stepExecution.StepExecutionAttempts.First());
            result = await stepExecutor.RunAsync(context, stepExecution, cts);
        }
        catch (OperationCanceledException)
        {
            // We should only arrive here if the step was canceled while it was Queued.
            // If the step was canceled once its execution had started, then the step's executor
            // should handle the cancellation and the result is returned normally from RunAsync().
            try
            {
                await UpdateExecutionCancelledAsync(stepExecution, cts.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating step execution status to cancelled");
            }
        }
        catch (Exception ex1)
        {
            try
            {
                await UpdateExecutionFailedAsync(ex1, stepExecution);
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Error updating step execution status to failed");
            }
        }
        finally
        {
            var status = result ? OrchestrationStatus.Succeeded : OrchestrationStatus.Failed;
            UpdateStatus(stepExecution, status);

            await listener.OnPostExecuteAsync(stepExecution);
        }
    }

    private void UpdateStatus(StepExecution step, OrchestrationStatus status)
    {
        lock (_lock)
        {
            _stepStatuses[step] = status;

            // Make a copy of the list as observers might unsubscribe during enumeration.
            foreach (var observer in _observers.ToArray())
            {
                observer.OnUpdate(new OrchestrationUpdate(step, status));
            }
        }
    }

    private async Task UpdateExecutionCancelledAsync(StepExecution stepExecution, string username)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            attempt.StartedOn ??= DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.StoppedBy = username;
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
            await context.StepExecutionAttempts
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                    .SetProperty(p => p.StartedOn, attempt.StartedOn)
                    .SetProperty(p => p.EndedOn, attempt.EndedOn)
                    .SetProperty(p => p.InfoMessages, attempt.InfoMessages)
                    .SetProperty(p => p.WarningMessages, attempt.WarningMessages)
                    .SetProperty(p => p.ErrorMessages, attempt.ErrorMessages)
                    .SetProperty(p => p.StoppedBy, attempt.StoppedBy), CancellationToken.None);
        }
    }

    private async Task UpdateExecutionFailedAsync(Exception ex, StepExecution stepExecution)
    {
        var attempt = stepExecution.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
        if (attempt is null)
        {
            return;
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        attempt.ExecutionStatus = StepExecutionStatus.Failed;
        attempt.StartedOn ??= DateTimeOffset.Now;
        attempt.EndedOn = DateTimeOffset.Now;
        // Place the error message first on the list.
        attempt.AddError(ex, $"Unhandled error caught in global orchestrator:\n\n{ex.Message}", insertFirst: true);
        await context.StepExecutionAttempts
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                    .SetProperty(p => p.StartedOn, attempt.StartedOn)
                    .SetProperty(p => p.EndedOn, attempt.EndedOn)
                    .SetProperty(p => p.InfoMessages, attempt.InfoMessages)
                    .SetProperty(p => p.WarningMessages, attempt.WarningMessages)
                    .SetProperty(p => p.ErrorMessages, attempt.ErrorMessages), CancellationToken.None);
    }

    private async Task UpdateStepAsync(StepExecution step, StepExecutionStatus status, string? errorMessage)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var attempt in step.StepExecutionAttempts)
        {
            attempt.ExecutionStatus = status;
            attempt.StartedOn = DateTimeOffset.Now;
            attempt.EndedOn = DateTimeOffset.Now;
            attempt.AddError(errorMessage);
            await context.StepExecutionAttempts
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ExecutionStatus, attempt.ExecutionStatus)
                    .SetProperty(p => p.StartedOn, attempt.StartedOn)
                    .SetProperty(p => p.EndedOn, attempt.EndedOn)
                    .SetProperty(p => p.InfoMessages, attempt.InfoMessages)
                    .SetProperty(p => p.WarningMessages, attempt.WarningMessages)
                    .SetProperty(p => p.ErrorMessages, attempt.ErrorMessages), CancellationToken.None);
        }
    }

    private async Task AddMonitorsAsync(IEnumerable<StepExecutionMonitor> monitors)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var distinct = monitors
                .DistinctBy(t =>
                    (t.ExecutionId,
                        t.StepId,
                        t.MonitoredExecutionId,
                        t.MonitoredStepId,
                        TrackingReason: t.MonitoringReason));
            context.StepExecutionMonitors.AddRange(distinct);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving new step execution monitors");
        }
    }
}
