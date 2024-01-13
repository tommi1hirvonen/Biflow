using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using Microsoft.Extensions.Logging;

namespace Biflow.Ui.Core;

public class ExecutionJob(
    IExecutionManager executionManager,
    ILogger<ExecutionJob> logger,
    IDbContextFactory<SchedulerDbContext> dbContextFactory,
    IExecutionBuilderFactory<SchedulerDbContext> executionBuilderFactory)
    : ExecutionJobBase(logger, dbContextFactory, executionBuilderFactory)
{
    private readonly IExecutionManager _executionManager = executionManager;

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        await _executionManager.StartExecutionAsync(executionId);
    }

    protected override async Task WaitForExecutionToFinish(Guid executionId)
    {
        await _executionManager.WaitForTaskCompleted(executionId, CancellationToken.None);
    }
}
