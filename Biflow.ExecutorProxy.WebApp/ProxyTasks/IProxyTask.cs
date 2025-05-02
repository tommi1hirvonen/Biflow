namespace Biflow.ExecutorProxy.WebApp.ProxyTasks;

internal interface IProxyTask<out TStatus, TResult>
{
    public TStatus Status { get; }
    
    public Task<TResult> RunAsync(CancellationToken cancellationToken);
}