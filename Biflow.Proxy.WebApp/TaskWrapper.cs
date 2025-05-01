namespace Biflow.Proxy.WebApp;

internal record TaskWrapper<T>(
    Task<T> Task,
    CancellationTokenSource CancellationTokenSource,
    DateTime? CompletedAt) : IDisposable
{
    public void Dispose()
    {
        CancellationTokenSource.Dispose();
    }
}