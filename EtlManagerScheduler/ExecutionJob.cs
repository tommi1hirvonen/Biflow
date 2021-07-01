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

        public ExecutionJob(ILogger<ExecutionJob> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var etlManagerConnectionString = _configuration.GetValue<string>("EtlManagerConnectionString")
                    ?? throw new ArgumentNullException("etlManagerConnectionString", "Connection string cannot be null");
                var executorFilePath = _configuration.GetValue<string>("EtlManagerExecutorPath")
                    ?? throw new ArgumentNullException("executorFilePath", "Executor file path cannot be null");

                var jobId = context.JobDetail.Key.Name;
                var scheduleId = context.Trigger.Key.Name;

                using var sqlConnection = new SqlConnection(etlManagerConnectionString);
                await sqlConnection.OpenAsync();

                try
                {
                    using var jobEnabledCommand = new SqlCommand("SELECT IsEnabled FROM etlmanager.Job WHERE JobId = @JobId", sqlConnection);
                    jobEnabledCommand.Parameters.AddWithValue("@JobId", jobId);
                    var isEnabled = (bool)(await jobEnabledCommand.ExecuteScalarAsync())!;
                    if (!isEnabled)
                        return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting job IsEnabled status");
                    return;
                }

                string executionId;
                try
                {
                    using var initCommand = new SqlCommand("EXEC etlmanager.ExecutionInitialize @JobId = @JobId_, @ScheduleId = @ScheduleId_", sqlConnection);
                    initCommand.Parameters.AddWithValue("@JobId_", jobId);
                    initCommand.Parameters.AddWithValue("@ScheduleId_", scheduleId);
                    executionId = (await initCommand.ExecuteScalarAsync())!.ToString()!;
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

                try
                {
                    using var processIdCmd = new SqlCommand("UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection);
                    processIdCmd.Parameters.AddWithValue("@ProcessId", executorProcess.Id);
                    processIdCmd.Parameters.AddWithValue("@ExecutionId", executionId);
                    await processIdCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating executor process id for execution {executionId}", executionId);
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
