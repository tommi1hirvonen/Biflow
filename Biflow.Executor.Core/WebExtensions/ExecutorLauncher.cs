namespace Biflow.Executor.Core.WebExtensions;

public class ExecutorLauncher : IExecutorLauncher
{
    private readonly IExecutionManager _executionManager;

    public ExecutorLauncher(IExecutionManager executionManager)
    {
        _executionManager = executionManager;
    }

    public async Task StartExecutorAsync(Guid executionId)
    {
        await _executionManager.StartExecutionAsync(executionId);
    }

    public async Task WaitForExitAsync(Guid executionId, CancellationToken cancellationToken) =>
        await _executionManager.WaitForTaskCompleted(executionId, cancellationToken);

    public Task CancelAsync(Guid executionId, string username)
    {
        _executionManager.CancelExecution(executionId, username);
        return Task.CompletedTask;
    }
}
