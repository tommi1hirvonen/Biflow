namespace EtlManager.Executor.Core.WebExtensions;

public class ExecutorLauncher : IExecutorLauncher
{
    private readonly ExecutionManager _executionManager;

    public ExecutorLauncher(ExecutionManager executionManager)
    {
        _executionManager = executionManager;
    }

    public Task StartExecutorAsync(Guid executionId, bool notify)
    {
        _executionManager.StartExecution(executionId, notify, null, false);
        return Task.CompletedTask;
    }

    public async Task WaitForExitAsync(Guid executionId, CancellationToken cancellationToken) =>
        await _executionManager.WaitForTaskCompleted(executionId, cancellationToken);

    public Task CancelAsync(Guid executionId, string username)
    {
        _executionManager.CancelExecution(executionId, username);
        return Task.CompletedTask;
    }
}
