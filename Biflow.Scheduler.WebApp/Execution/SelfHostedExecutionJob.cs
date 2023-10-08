using Biflow.DataAccess;
using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Scheduler.WebApp;

public class SelfHostedExecutionJob(
    IExecutionManager executionManager,
    ILogger<SelfHostedExecutionJob> logger,
    IDbContextFactory<SchedulerDbContext> dbContextFactory,
    IExecutionBuilderFactory<SchedulerDbContext> executionBuilderFactory) : ExecutionJobBase(logger, dbContextFactory, executionBuilderFactory)
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
