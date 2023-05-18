namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestrationObserver
{
    public void Subscribe(IOrchestrationObservable observable);

    public void OnStepExecutionStatusChange(StepExecutionStatusInfo statusChange);

    public Task WaitForOrchestrationAsync(IOrchestrationListener orchestrationListener, CancellationToken cancellationToken);
}
