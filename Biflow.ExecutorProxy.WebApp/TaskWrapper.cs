using Biflow.ExecutorProxy.WebApp.ProxyTasks;

namespace Biflow.ExecutorProxy.WebApp;

internal record TaskWrapper<TResult, TStatus>(
    IProxyTask<TStatus, TResult> ProxyTask,
    Task<TResult> Task,
    CancellationTokenSource CancellationTokenSource,
    DateTime? CompletedAt) : IDisposable
{
    public void Dispose()
    {
        CancellationTokenSource.Dispose();
    }
}