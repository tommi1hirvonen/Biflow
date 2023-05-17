namespace Biflow.Executor.Core.Orchestrator;

internal class Unsubscriber<TEventInfo> : IDisposable
{
    private readonly List<IObserver<TEventInfo>> _observers;
    private readonly IObserver<TEventInfo> _observer;

    public Unsubscriber(List<IObserver<TEventInfo>> observers, IObserver<TEventInfo> observer)
    {
        _observers = observers;
        _observer = observer;
    }

    public void Dispose()
    {
        _observers.Remove(_observer);
    }
}
