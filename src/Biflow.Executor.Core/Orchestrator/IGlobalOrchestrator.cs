namespace Biflow.Executor.Core.Orchestrator;

internal interface IGlobalOrchestrator : IOrchestrationObservable
{
    public Task RegisterStepsAndObserversAsync(
        OrchestrationContext context,
        ICollection<IOrchestrationObserver> observers,
        IStepExecutionListener stepExecutionListener);
}
