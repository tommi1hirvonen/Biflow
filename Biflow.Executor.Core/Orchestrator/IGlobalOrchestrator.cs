namespace Biflow.Executor.Core.Orchestrator;

internal interface IGlobalOrchestrator : IOrchestrationObservable
{
    public Task RegisterStepsAndObservers(List<IOrchestrationObserver> observers);
}
