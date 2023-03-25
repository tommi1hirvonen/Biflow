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

    protected override string BiflowConnectionString =>
        _configuration.GetConnectionString("BiflowContext") ?? throw new ArgumentNullException(nameof(BiflowConnectionString));

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        await _executionManager.StartExecutionAsync(executionId);
    }

    protected override async Task WaitForExecutionToFinish(Guid executionId)
    {
        await _executionManager.WaitForTaskCompleted(executionId, CancellationToken.None);
    }
}
