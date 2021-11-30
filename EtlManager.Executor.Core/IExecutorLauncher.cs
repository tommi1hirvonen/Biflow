namespace EtlManager.Executor.Core;

public interface IExecutorLauncher
{
    public Task StartExecutorAsync(Guid executionId, bool notify);

    public Task WaitForExitAsync(Guid executionId, CancellationToken cancellationToken);

    public Task CancelAsync(Guid executionId, string username);
}
