using Biflow.Proxy.WebApp.ProxyTasks;

namespace Biflow.Proxy.WebApp;

internal record TaskWrapper<TResult, TStatus>(
    ProxyTask<TStatus, TResult> ProxyTask,
    Task<TResult> Task,
    CancellationTokenSource CancellationTokenSource,
    DateTime? CompletedAt) : IDisposable
{
    public void Dispose()
    {
        CancellationTokenSource.Dispose();
    }
}