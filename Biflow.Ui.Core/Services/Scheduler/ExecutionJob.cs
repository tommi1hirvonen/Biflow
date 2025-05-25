using Biflow.Core;
using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Biflow.Ui.Core;

[UsedImplicitly]
public class ExecutionJob(
    IExecutionManager executionManager,
    ILogger<ExecutionJob> logger,
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
