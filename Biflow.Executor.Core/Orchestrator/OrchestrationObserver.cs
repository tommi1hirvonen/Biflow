using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.OrchestrationTracker;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class OrchestrationObserver(
    ILogger logger,
    StepExecution stepExecution,
    IStepExecutionListener orchestrationListener,
    IEnumerable<IOrchestrationTracker> orchestrationTrackers,
    ExtendedCancellationTokenSource cancellationTokenSource) : IOrchestrationObserver, IDisposable
{
    private readonly TaskCompletionSource<OrchestratorAction> _tcs = new();
    private readonly ILogger _logger = logger;
    private readonly IStepExecutionListener _orchestrationListener = orchestrationListener;
    private readonly IEnumerable<IOrchestrationTracker> _orchestrationTrackers = orchestrationTrackers;
    private readonly ExtendedCancellationTokenSource _cancellationTokenSource = cancellationTokenSource;
    private IDisposable? _unsubscriber;

    public StepExecution StepExecution { get; } = stepExecution;

    public int Priority => StepExecution.ExecutionPhase;

    public IEnumerable<StepExecutionMonitor> RegisterInitialUpdates(IEnumerable<OrchestrationUpdate> updates)
    {
        try
        {
            var monitors = updates
                .SelectMany(HandleUpdate)
                .ToArray();
            return monitors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while registering initial updates in orchestration observer");
            var action = Actions.Fail(StepExecutionStatus.Failed, "Error while registering initial updates in orchestration observer");
            SetResult(action);
            return [];
        }   
    }

    public void RegisterInitialUpdate(OrchestrationUpdate update)
    {
        try
        {
            HandleUpdate(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while registering an initial update in orchestration observer");
            var action = Actions.Fail(StepExecutionStatus.Failed, "Error while registering an initial updates in orchestration observer");
            SetResult(action);
        }
    }

    public Task? AfterInitialUpdatesRegisteredAsync(
        Func<StepExecution, IStepExecutionListener, ExtendedCancellationTokenSource, Task> executeCallback)
    {
        try
        {
            // The TaskCompletionSource result was set in RegisterInitialUpdates().
            // Do not request processing but instead let the observer continue to WaitForProcessingAsync().
            if (_tcs.Task.IsCompleted)
            {
                return null;
            }

            var action = GetStepAction();
            if (action?.Value is ExecuteAction)
            {
                // If the action was ExecuteAction already after registering initial updates, request execution.
                return executeCallback(StepExecution, _orchestrationListener, _cancellationTokenSource);
            }
            else if (action is not null)
            {
                // If the action was something else (fail, cancel), set the result and let the observer continue to WaitForProcessingAsync().
                SetResult(action);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while evaluating actions after registering initial updates");
            var action = Actions.Fail(StepExecutionStatus.Failed, "Error while evaluating actions after registering initial updates");
            SetResult(action);
            return null;
        }
    }

    public async Task WaitForProcessingAsync(
        Func<StepExecution, OrchestratorAction, IStepExecutionListener, ExtendedCancellationTokenSource, Task> processCallback)
    {
        OrchestratorAction stepAction;
        try
        {
            stepAction = await _tcs.Task.WaitAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            stepAction = Actions.Cancel;
        }
        await processCallback(StepExecution, stepAction, _orchestrationListener, _cancellationTokenSource);
    }

    public void Subscribe(IOrchestrationObservable provider)
    {
        _unsubscriber = provider.Subscribe(this);
    }

    public IEnumerable<StepExecutionMonitor> OnUpdate(OrchestrationUpdate value)
    {
        try
        {
            var monitors = HandleUpdate(value);
            var action = GetStepAction();
            if (action is not null)
            {
                SetResult(action);
            }
            return monitors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling update in orchestration observer");
            var action = Actions.Fail(StepExecutionStatus.Failed, $"Error while handling update in orchestration observer");
            SetResult(action);
            return [];
        }
    }

    /// <summary>
    /// Called multiple times in succession when the observer is registering
    /// initial statuses of all steps from global orchestration.
    /// After that it is called once whenever orchestration updates are provided.
    /// </summary>
    /// <param name="value"></param>
    private IEnumerable<StepExecutionMonitor> HandleUpdate(OrchestrationUpdate value)
    {
        return _orchestrationTrackers.Select(t => t.HandleUpdate(value))
            .WhereNotNull()
            .ToArray();
    }

    /// <summary>
    /// Called once after HandleUpdate() has been called for all initial statuses.
    /// After that it is called once every time after HandleUpdate() has been called.
    /// </summary>
    /// <returns>null if no action should be taken with the step at this time. Otherwise a valid OrchestratorAction should be provided.</returns>
    private OrchestratorAction? GetStepAction()
    {
        // For each tracker, get the step action.
        foreach (var tracker in _orchestrationTrackers)
        {
            var action = tracker.GetStepAction().Match<OrchestratorAction?>(
                (WaitAction wait) => null,
                (ExecuteAction execute) => execute,
                (CancelAction cancel) => cancel,
                (FailAction fail) => fail);
            // If the action is one of fail, cancel or wait (null), break and return early.
            if (action?.Value is FailAction or CancelAction or null)
            {
                return action;
            }
        }

        // All trackers have been iterated over and none reported fail, cancel or wait.
        // The only remaining possibility then is to execute.
        return Actions.Execute;
    }

    private void SetResult(OrchestratorAction action)
    {
        _unsubscriber?.Dispose();
        _unsubscriber = null;
        _tcs.TrySetResult(action);
    }

    public void Dispose()
    {
        _unsubscriber?.Dispose();
        _unsubscriber = null;
    }
}
