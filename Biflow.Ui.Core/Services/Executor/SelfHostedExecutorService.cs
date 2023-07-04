using Biflow.Executor.Core.WebExtensions;

namespace Biflow.Ui.Core;

public class SelfHostedExecutorService : IExecutorService
{
    private readonly ExecutionManager _executionManager;

    public SelfHostedExecutorService(ExecutionManager executionManager)
    {
        _executionManager = executionManager;
    }

    public async Task StartExecutionAsync(Guid executionId)
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
