using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal abstract class OrchestrationObserver(
    StepExecution stepExecution,
    IStepExecutionListener orchestrationListener,
    ExtendedCancellationTokenSource cancellationTokenSource) : IOrchestrationObserver, IDisposable
{
    private readonly TaskCompletionSource<StepAction> _tcs = new();
    private readonly IStepExecutionListener _orchestrationListener = orchestrationListener;
    private readonly ExtendedCancellationTokenSource _cancellationTokenSource = cancellationTokenSource;
    private IDisposable? _unsubscriber;

    public StepExecution StepExecution { get; } = stepExecution;

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
    protected abstract void HandleUpdate(OrchestrationUpdate value);

    /// <summary>
    /// Called once after HandleUpdate() has been called for all initial statuses.
    /// After that it is called once every time after HandleUpdate() has been called.
    /// </summary>
    /// <returns>null if no action should be taken with the step at this time. Otherwise a valid StepAction should be provided.</returns>
    protected abstract StepAction? GetStepAction();

    private void SetResult(StepAction action)
    {
        _unsubscriber?.Dispose();
        _unsubscriber = null;
        _tcs.TrySetResult(action);
    }
}
