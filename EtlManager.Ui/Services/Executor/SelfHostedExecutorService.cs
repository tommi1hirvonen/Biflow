using EtlManager.DataAccess.Models;
using EtlManager.Executor.WebApp;

namespace EtlManager.Ui.Services;

public class SelfHostedExecutorService : IExecutorService
{
    private readonly ExecutionManager _executionManager;

    public SelfHostedExecutorService(ExecutionManager executionManager)
    {
        _executionManager = executionManager;
    }

    public Task StartExecutionAsync(Guid executionId, bool notify, SubscriptionType? notifyMe, bool notifyMeOvertime)
    {
        _executionManager.StartExecution(executionId, notify, notifyMe, notifyMeOvertime);
        return Task.CompletedTask;
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
