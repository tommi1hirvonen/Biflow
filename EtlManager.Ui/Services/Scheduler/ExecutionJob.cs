using EtlManager.DataAccess;
using EtlManager.Executor.Core.WebExtensions;
using EtlManager.Scheduler.Core;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Services;

public class ExecutionJob : ExecutionJobBase
{
    private readonly IConfiguration _configuration;
    private readonly ExecutionManager _executionManager;


    public ExecutionJob(IConfiguration configuration, ExecutionManager executionManager,
        ILogger<ExecutionJob> logger, IDbContextFactory<EtlManagerContext> dbContextFactory)
        : base(logger, dbContextFactory)
    {
        _configuration = configuration;
        _executionManager = executionManager;
    }

    protected override string EtlManagerConnectionString => _configuration.GetConnectionString("EtlManagerContext");

    protected override Task StartExecutorAsync(Guid executionId)
    {
        _executionManager.StartExecution(executionId);
        return Task.CompletedTask;
    }

    protected override async Task WaitForExecutionToFinish(Guid executionId)
    {
        await _executionManager.WaitForTaskCompleted(executionId, CancellationToken.None);
    }
}
