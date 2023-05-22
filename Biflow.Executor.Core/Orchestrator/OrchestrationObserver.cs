using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal abstract class OrchestrationObserver : IOrchestrationObserver, IDisposable
{
    private readonly TaskCompletionSource<StepAction> _tcs = new();
    private readonly IStepProcessingListener _orchestrationListener;
    private readonly ExtendedCancellationTokenSource _cancellationTokenSource;
    private IDisposable? _unsubscriber;

    public StepExecution StepExecution { get; }

    public OrchestrationObserver(StepExecution stepExecution, IStepProcessingListener orchestrationListener, ExtendedCancellationTokenSource cancellationTokenSource)
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

    public abstract void RegisterInitialUpdates(IEnumerable<OrchestrationUpdate> initialStatuses);

    public abstract void OnUpdate(OrchestrationUpdate value);

    public async Task WaitForProcessingAsync(IStepReadyForProcessingListener stepReadyListener)
    {
        StepAction stepAction;
        try
        {
            stepAction = await _tcs.Task.WaitAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            stepAction = StepAction.Cancel;
        }
        await stepReadyListener.OnStepReadyForProcessingAsync(StepExecution, stepAction, _orchestrationListener, _cancellationTokenSource);
    }

    protected void SetResult(StepAction action)
    {
        if (action != StepAction.Wait)
        {
            _unsubscriber?.Dispose();
            _unsubscriber = null;
            _tcs.TrySetResult(action);
        }
    }
}
