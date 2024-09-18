namespace Biflow.Executor.Core.Orchestrator;

internal interface IGlobalOrchestrator : IOrchestrationObservable
{
    public Task RegisterStepsAndObserversAsync(IEnumerable<IOrchestrationObserver> observers);
}
