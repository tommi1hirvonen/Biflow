namespace Biflow.Proxy.WebApp.ProxyTasks;

public abstract class ProxyTask<TStatus, TResult>
{
    public abstract TStatus Status { get; }
    
    public abstract Task<TResult> RunAsync(CancellationToken cancellationToken);
}