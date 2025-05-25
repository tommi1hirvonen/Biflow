using Biflow.Core;
using Biflow.DataAccess;
using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Scheduler.WebApp;

[UsedImplicitly]
public class SelfHostedExecutionJob(
    IExecutionManager executionManager,
    ILogger<SelfHostedExecutionJob> logger,
    [FromKeyedServices(SchedulerServiceKeys.JobStartFailuresHealthService)]
    HealthService healthService,
    IDbContextFactory<SchedulerDbContext> dbContextFactory,
    IExecutionBuilderFactory<SchedulerDbContext> executionBuilderFactory)
    : ExecutionJobBase(logger, healthService, dbContextFactory, executionBuilderFactory)
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
