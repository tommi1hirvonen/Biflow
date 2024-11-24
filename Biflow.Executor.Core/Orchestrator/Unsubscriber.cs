namespace Biflow.Executor.Core.Orchestrator;

internal class Unsubscriber(List<IOrchestrationObserver> observers, IOrchestrationObserver observer) : IDisposable
{
    public void Dispose()
    {
        observers.Remove(observer);
    }
}
