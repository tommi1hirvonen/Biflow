using Biflow.Executor.Core.OrchestrationObserver;

namespace Biflow.Executor.Core.Orchestrator;

internal class Unsubscriber(List<IOrchestrationObserver> observers, IOrchestrationObserver observer) : IDisposable
{
    private readonly List<IOrchestrationObserver> _observers = observers;
    private readonly IOrchestrationObserver _observer = observer;

    public void Dispose()
    {
        _observers.Remove(_observer);
    }
}
