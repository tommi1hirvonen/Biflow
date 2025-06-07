namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestrationObservable
{
    public IDisposable Subscribe(IOrchestrationObserver observer);
}
