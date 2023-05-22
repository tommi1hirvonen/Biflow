using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal abstract class OrchestrationObserver : IOrchestrationObserver, IDisposable
{
    private readonly TaskCompletionSource<StepAction> _tcs = new();
    private readonly IStepExecutionListener _orchestrationListener;
    private readonly ExtendedCancellationTokenSource _cancellationTokenSource;
    private IDisposable? _unsubscriber;

    public StepExecution StepExecution { get; }

    public OrchestrationObserver(StepExecution stepExecution, IStepExecutionListener orchestrationListener, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        StepExecution = stepExecution;
        _orchestrationListener = orchestrationListener;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public void Subscribe(IOrchestrationObservable provider)
    {
        _unsubscriber = provider.Subscribe(this);
    }

    public void Dispose() => _unsubscriber?.Dispose();

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

    protected abstract void HandleUpdate(OrchestrationUpdate value);

    protected abstract StepAction? GetStepAction();

    private void SetResult(StepAction action)
    {
        _unsubscriber?.Dispose();
        _unsubscriber = null;
        _tcs.TrySetResult(action);
    }
}
