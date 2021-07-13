using Dapper;
using EtlManagerDataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerScheduler
{
    [DisallowConcurrentExecution]
    class ExecutionJob : IJob
    {
        private readonly ILogger<ExecutionJob> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

        public ExecutionJob(ILogger<ExecutionJob> logger, IConfiguration configuration, IDbContextFactory<EtlManagerContext> dbContextFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _dbContextFactory = dbContextFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var etlManagerConnectionString = _configuration.GetConnectionString("EtlManagerContext")
                    ?? throw new ArgumentNullException("etlManagerConnectionString", "Connection string cannot be null");
                var executorFilePath = _configuration.GetValue<string>("EtlManagerExecutorPath")
                    ?? throw new ArgumentNullException("executorFilePath", "Executor file path cannot be null");

                var jobId = Guid.Parse(context.JobDetail.Key.Name);
                var scheduleId = Guid.Parse(context.Trigger.Key.Name);

                try
                {
                    using var dbContext = _dbContextFactory.CreateDbContext();
                    var isEnabled = await dbContext.Jobs
                        .Where(job => job.JobId == jobId)
                        .Select(job => job.IsEnabled)
                        .FirstAsync();
                    if (!isEnabled)
                        return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting job IsEnabled status");
                    return;
                }

                using var sqlConnection = new SqlConnection(etlManagerConnectionString);
                await sqlConnection.OpenAsync();

                Guid executionId;
                try
                {
                    executionId = await sqlConnection.ExecuteScalarAsync<Guid>(
                        "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_, @ScheduleId = @ScheduleId_",
                        new { JobId_ = jobId, ScheduleId_ = scheduleId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing execution for job {jobId}", jobId);
                    return;
                }

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

                var executorProcess = new Process() { StartInfo = executionInfo };
                try
                {
                    executorProcess.Start();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting executor process for execution {executionId}", executionId);
                    return;
                }

                _logger.LogInformation($"Started execution for job id {jobId}, schedule id {scheduleId}, execution id {executionId}");

                // Wait for the execution to finish and for the executor process to exit.
                // This way Quartz does not start a parallel execution of the same job.
                await executorProcess.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quartz scheduled job threw an error");
            }
        }
    }
}
