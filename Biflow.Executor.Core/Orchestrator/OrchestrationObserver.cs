using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.OrchestrationTracker;

namespace Biflow.Executor.Core.Orchestrator;

internal class OrchestrationObserver(
    StepExecution stepExecution,
    IStepExecutionListener orchestrationListener,
    IEnumerable<IOrchestrationTracker> orchestrationTrackers,
    ExtendedCancellationTokenSource cancellationTokenSource) : IOrchestrationObserver, IDisposable
{
    private readonly TaskCompletionSource<StepAction> _tcs = new();
    private readonly IStepExecutionListener _orchestrationListener = orchestrationListener;
    private readonly IEnumerable<IOrchestrationTracker> _orchestrationTrackers = orchestrationTrackers;
    private readonly ExtendedCancellationTokenSource _cancellationTokenSource = cancellationTokenSource;
    private IDisposable? _unsubscriber;

    public StepExecution StepExecution { get; } = stepExecution;

    public int Priority => StepExecution.ExecutionPhase;

    public void Subscribe(IOrchestrationObservable provider)
    {
        _unsubscriber = provider.Subscribe(this);
    }

    public void Dispose()
    {
        _unsubscriber?.Dispose();
        _unsubscriber = null;
    }

    public void RegisterInitialUpdates(IEnumerable<OrchestrationUpdate> initialStatuses)
    {
        foreach (var status in initialStatuses)
        {
            HandleUpdate(status);
        }
        var action = GetStepAction();
        if (action is not null)
        {
            SetResult(action);
        }
    }

    public void OnUpdate(OrchestrationUpdate value)
    {
        HandleUpdate(value);
        var action = GetStepAction();
        if (action is not null)
        {
            SetResult(action);
        }
    }

    public async Task WaitForProcessingAsync(IStepReadyForProcessingListener stepReadyListener)
    {
        StepAction stepAction;
        try
        {
            stepAction = await _tcs.Task.WaitAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            stepAction = new Cancel();
        }
        await stepReadyListener.OnStepReadyForProcessingAsync(StepExecution, stepAction, _orchestrationListener, _cancellationTokenSource);
    }

    /// <summary>
    /// Called multiple times in succession when the observer is registering
    /// initial statuses of all steps from global orchestration.
    /// After that it is called once whenever orchestration updates are provided.
    /// </summary>
    /// <param name="value"></param>
    private void HandleUpdate(OrchestrationUpdate value)
    {
        foreach (var tracker in _orchestrationTrackers)
        {
            tracker.HandleUpdate(value);
        }
    }

    /// <summary>
    /// Called once after HandleUpdate() has been called for all initial statuses.
    /// After that it is called once every time after HandleUpdate() has been called.
    /// </summary>
    /// <returns>null if no action should be taken with the step at this time. Otherwise a valid StepAction should be provided.</returns>
    private StepAction? GetStepAction()
    {
        foreach (var tracker in _orchestrationTrackers)
        {
            var action = tracker.GetStepAction();
            if (FailOrNull(action) is Fail fail)
            {
                return fail;
            }
            if (CancelOrNull(action) is Cancel cancel)
            {
                return cancel;
            }
            if (action is null)
            {
                return null;
            }
        }
        return new Execute();
    }

    private static Fail? FailOrNull(StepAction? action) => action?
        .Match<Fail?>(
            (execute) => null,
            (cancel) => null,
            (fail) => fail);

    private static Cancel? CancelOrNull(StepAction? action) => action?
        .Match<Cancel?>(
            (execute) => null,
            (cancel) => cancel,
            (fail) => null);

    private void SetResult(StepAction action)
    {
        _unsubscriber?.Dispose();
        _unsubscriber = null;
        _tcs.TrySetResult(action);
    }
}
