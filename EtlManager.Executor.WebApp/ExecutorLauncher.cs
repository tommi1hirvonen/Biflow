using EtlManager.Executor.Core;
using EtlManager.Executor.Core.JobExecutor;
using EtlManager.Utilities;

namespace EtlManager.Executor.WebApp;

internal class ExecutorLauncher : IExecutorLauncher
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
        var command = new StartCommand
        {
            ExecutionId = executionId,
            NotifyMe = null,
            Notify = notify,
            NotifyMeOvertime = false
        };
        _executionManager.StartExecution(command, _jobExecutor);
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
