using Biflow.DataAccess.Models;
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

    public Task StopExecutionAsync(StepExecutionAttempt attempt, string username)
    {
        _executionManager.CancelExecution(attempt.ExecutionId, username, attempt.StepId);
        return Task.CompletedTask;
    }

    public Task StopExecutionAsync(Execution execution, string username)
    {
        _executionManager.CancelExecution(execution.ExecutionId, username);
        return Task.CompletedTask;
    }
}
