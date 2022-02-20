using Biflow.DataAccess;
using Biflow.Executor.Core.WebExtensions;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Biflow.Ui.Core;

public class ExecutionJob : ExecutionJobBase
{
    private readonly IConfiguration _configuration;
    private readonly ExecutionManager _executionManager;


    public ExecutionJob(IConfiguration configuration, ExecutionManager executionManager,
        ILogger<ExecutionJob> logger, IDbContextFactory<BiflowContext> dbContextFactory)
        : base(logger, dbContextFactory)
    {
        _configuration = configuration;
        _executionManager = executionManager;
    }

    protected override string BiflowConnectionString => _configuration.GetConnectionString("BiflowContext");

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
