using Biflow.Core;
using Biflow.DataAccess;
using Biflow.Scheduler.Core;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Biflow.Scheduler.WebApp;

public class ConsoleAppExecutionJob : ExecutionJobBase
{
    private readonly ILogger<ConsoleAppExecutionJob> _logger;
    private readonly IConfiguration _configuration;

    public ConsoleAppExecutionJob(
        ILogger<ConsoleAppExecutionJob> logger,
        IConfiguration configuration,
        IDbContextFactory<BiflowContext> dbContextFactory,
        ISqlConnectionFactory sqlConnectionFactory)
        : base(logger, dbContextFactory, sqlConnectionFactory)
    {
        _logger = logger;
        _configuration = configuration;
    }

    private Process? ExecutorProcess { get; set; }

    protected override async Task StartExecutorAsync(Guid executionId)
    {
        var executorFilePath = _configuration.GetSection("Executor").GetSection("ConsoleApp").GetValue<string>("BiflowExecutorPath")
                ?? throw new ArgumentNullException("executorFilePath", "Executor file path cannot be null");
        var executionInfo = new ProcessStartInfo()
        {
            FileName = executorFilePath,
            ArgumentList = {
                        "execute",
                        "--id",
                        executionId.ToString()
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
