namespace Biflow.Executor.Core.Orchestrator;

internal interface IGlobalOrchestrator : IOrchestrationObservable
{
    public Task RegisterStepsAndObserversAsync(
        ICollection<IOrchestrationObserver> observers,
        IStepExecutionListener stepExecutionListener);
}
