using EtlManager.Executor.Core;
using EtlManager.Executor.Core.JobExecutor;

namespace EtlManager.Executor.WebApp;

public class ExecutorLauncher : IExecutorLauncher
{
    private readonly ExecutionManager _executionManager;
    private readonly IJobExecutor _jobExecutor;

    public ExecutorLauncher(ExecutionManager executionManager, IJobExecutor jobExecutor)
    {
        _executionManager = executionManager;
        _jobExecutor = jobExecutor;
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
