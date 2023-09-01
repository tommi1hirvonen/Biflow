namespace Biflow.Executor.Core.Orchestrator;

internal interface IGlobalOrchestrator : IOrchestrationObservable
{
    public Task RegisterStepsAndObservers(IEnumerable<IOrchestrationObserver> observers);
}
