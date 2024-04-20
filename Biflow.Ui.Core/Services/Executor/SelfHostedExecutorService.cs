using Biflow.Executor.Core;

namespace Biflow.Ui.Core;

public class SelfHostedExecutorService(IExecutionManager executionManager) : IExecutorService
{
    private readonly IExecutionManager _executionManager = executionManager;

    public async Task StartExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        await _executionManager.StartExecutionAsync(executionId);
    }

    public Task StopExecutionAsync(Guid executionId, Guid stepId, string username)
    {
        _executionManager.CancelExecution(executionId, username, stepId);
        return Task.CompletedTask;
    }

    public Task StopExecutionAsync(Guid executionId, string username)
    {
        _executionManager.CancelExecution(executionId, username);
        return Task.CompletedTask;
    }
}
