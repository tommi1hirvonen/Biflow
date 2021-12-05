namespace EtlManager.Executor.Core;

public interface IExecutorLauncher
{
    public Task StartExecutorAsync(Guid executionId);

    public Task WaitForExitAsync(Guid executionId, CancellationToken cancellationToken);

    public Task CancelAsync(Guid executionId, string username);
}
