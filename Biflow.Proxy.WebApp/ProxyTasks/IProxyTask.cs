namespace Biflow.Proxy.WebApp.ProxyTasks;

internal interface IProxyTask<out TStatus, TResult>
{
    public TStatus Status { get; }
    
    public Task<TResult> RunAsync(CancellationToken cancellationToken);
}