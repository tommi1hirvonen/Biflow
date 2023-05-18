namespace Biflow.Executor.Core.Orchestrator;

internal interface IGlobalOrchestrator : IOrchestrationObservable
{
    public IEnumerable<Task> RegisterStepsAndObservers(IEnumerable<IOrchestrationObserver> observers);
}
