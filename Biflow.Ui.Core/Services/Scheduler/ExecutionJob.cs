using Biflow.DataAccess;
using Biflow.Executor.Core.WebExtensions;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Ui.Core;

public class ExecutionJob : ExecutionJobBase
{
    private readonly ExecutionManager _executionManager;


    public ExecutionJob(ExecutionManager executionManager,
        ILogger<ExecutionJob> logger, IDbContextFactory<BiflowContext> dbContextFactory, IExecutionBuilderFactory executionBuilderFactory)
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
