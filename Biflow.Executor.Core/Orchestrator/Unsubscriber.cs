namespace Biflow.Executor.Core.Orchestrator;

internal class Unsubscriber : IDisposable
{
    private readonly List<IOrchestrationObserver> _observers;
    private readonly IOrchestrationObserver _observer;

    public Unsubscriber(List<IOrchestrationObserver> observers, IOrchestrationObserver observer)
    {
        _observers = observers;
        _observer = observer;
    }

    public void Dispose()
    {
        _observers.Remove(_observer);
    }
}
