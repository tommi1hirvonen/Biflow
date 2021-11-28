using EtlManager.DataAccess;
using EtlManager.Scheduler.Core;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Scheduler.WebApp;

public class WebAppExecutionJob : ExecutionJob
{
    private readonly IConfiguration _configuration;

    public WebAppExecutionJob(IConfiguration configuration, ILogger logger, IDbContextFactory<EtlManagerContext> dbContextFactory)
        : base(logger, dbContextFactory)
    {
        _configuration = configuration;
    }

    protected override string EtlManagerConnectionString => _configuration.GetConnectionString("EtlManagerContext")
                ?? throw new ArgumentNullException("EtlManagerConnectionString", "Connection string cannot be null");

    protected override Task StartExecutorAsync(Guid executionId)
    {
        throw new NotImplementedException();
    }

    protected override Task WaitForExecutionToFinish(Guid executionId)
    {
        throw new NotImplementedException();
    }
}
