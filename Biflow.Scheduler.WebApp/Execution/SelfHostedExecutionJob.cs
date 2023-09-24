using Biflow.DataAccess;
using Biflow.Executor.Core;
using Biflow.Executor.Core.WebExtensions;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Scheduler.WebApp;

internal class SelfHostedExecutionJob : ExecutionJobBase
{
    private readonly IExecutionManager _executionManager;

    public SelfHostedExecutionJob(
        IExecutionManager executionManager,
        ILogger<SelfHostedExecutionJob> logger,
        IDbContextFactory<SchedulerDbContext> dbContextFactory,
        IExecutionBuilderFactory executionBuilderFactory)
        : base(logger, dbContextFactory, executionBuilderFactory)
    {
        _executionManager = executionManager;
    }

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        await _executionManager.StartExecutionAsync(executionId);
    }

    protected override async Task WaitForExecutionToFinish(Guid executionId)
    {
        await _executionManager.WaitForTaskCompleted(executionId, CancellationToken.None);
    }
}
