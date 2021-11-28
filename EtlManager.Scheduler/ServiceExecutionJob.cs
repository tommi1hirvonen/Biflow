using EtlManager.DataAccess;
using EtlManager.Scheduler.Core;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Diagnostics;

namespace EtlManager.Scheduler;

public class ServiceExecutionJob : ExecutionJob
{
    private readonly ILogger<ServiceExecutionJob> _logger;
    private readonly IConfiguration _configuration;

    public ServiceExecutionJob(ILogger<ServiceExecutionJob> logger, IConfiguration configuration, IDbContextFactory<EtlManagerContext> dbContextFactory)
        : base(logger, dbContextFactory)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private Process? ExecutorProcess { get; set; }

    protected override string EtlManagerConnectionString => _configuration.GetConnectionString("EtlManagerContext")
                ?? throw new ArgumentNullException("EtlManagerConnectionString", "Connection string cannot be null");

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        var executorFilePath = _configuration.GetValue<string>("EtlManagerExecutorPath")
                ?? throw new ArgumentNullException("executorFilePath", "Executor file path cannot be null");
        var executionInfo = new ProcessStartInfo()
        {
            FileName = executorFilePath,
            ArgumentList = {
                        "execute",
                        "--id",
                        executionId.ToString(),
                        "--notify"
                    },
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        ExecutorProcess = new Process() { StartInfo = executionInfo };
        try
        {
            ExecutorProcess.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting executor process for execution {executionId}", executionId);
            return;
        }
        await Task.CompletedTask;
    }

    protected override async Task WaitForExecutionToFinish(Guid executionId)
    {
        await (ExecutorProcess?.WaitForExitAsync() ?? Task.CompletedTask);
    }
}
