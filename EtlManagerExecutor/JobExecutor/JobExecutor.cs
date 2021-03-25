using EtlManagerUtils;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class JobExecutor : IJobExecutor
    {
        private readonly IConfiguration configuration;

        public JobExecutor(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task RunAsync(string executionId, bool notify)
        {
            var connectionString = configuration.GetValue<string>("EtlManagerConnectionString");
            var pollingIntervalMs = configuration.GetValue<int>("PollingIntervalMs");
            var maxParallelSteps = configuration.GetValue<int>("MaximumParallelSteps");
            var encryptionId = configuration.GetValue<string>("EncryptionId");

            string encryptionPassword;
            try
            {
                encryptionPassword = await CommonUtility.GetEncryptionKeyAsync(encryptionId, connectionString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting encryption password", executionId);
                return;
            }

            bool dependencyMode;
            Job job;

            using (var sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();

                // Update this Executor process's PID for the execution.
                var process = Process.GetCurrentProcess();
                using (var processIdCmd = new SqlCommand("UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection))
                {
                    processIdCmd.Parameters.AddWithValue("@ProcessId", process.Id);
                    processIdCmd.Parameters.AddWithValue("@ExecutionId", executionId);
                    await processIdCmd.ExecuteNonQueryAsync();
                }
                
                // Get execution details.
                using var detailsCommand = new SqlCommand(@"SELECT TOP 1 DependencyMode, JobId, JobName FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
                detailsCommand.Parameters.AddWithValue("@ExecutionId", executionId);
                using var reader = await detailsCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // Get whether the execution should be run in dependency mode or in execution phase mode.
                    dependencyMode = (bool)reader["DependencyMode"];

                    // Get job details for the execution.
                    var jobId = reader["JobId"].ToString();
                    var jobName = reader["JobName"].ToString();
                    job = new(jobId, jobName);
                }
                else
                {
                    Log.Error("{executionId} No execution initialized with given execution id", executionId);
                    return;
                }
            }

            var executionConfig = new ExecutionConfiguration(
                connectionString: connectionString,
                encryptionKey: encryptionPassword,
                maxParallelSteps: maxParallelSteps,
                pollingIntervalMs: pollingIntervalMs,
                executionId: executionId,
                job: job,
                notify: notify,
                // Set the username as timeout. If steps are to be canceled, this will be used by default.
                username: "timeout");

            // Check whether there are circular dependencies between jobs (through steps executing another jobs).
            string circularExecutions;
            try
            {
                circularExecutions = await GetCircularJobExecutionsAsync(executionConfig);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error checking for possible circular job executions", executionId);
                return;
            }

            if (!string.IsNullOrEmpty(circularExecutions))
            {
                await Utility.UpdateErrorMessageAsync(executionConfig, "Execution was cancelled because of circular job executions:\n" + circularExecutions);
                Log.Error("{executionId} Execution was cancelled because of circular job executions: " + circularExecutions, executionId);
                return;
            }

            ExecutorBase executor;
            if (dependencyMode)
            {
                Log.Information("{ExecutionId} Starting execution in dependency mode", executionId);
                executor = new DependencyModeExecutor(executionConfig);
            }
            else
            {
                Log.Information("{executionId} Starting execution in execution phase mode", executionId);
                executor = new ExecutionPhaseExecutor(executionConfig);
            }
            await executor.RunAsync();

            // Execution finished. Notify subscribers of possible errors.
            if (notify)
            {
                EmailHelper.SendNotification(configuration, executionId);
            }
        }

        private static async Task<string> GetCircularJobExecutionsAsync(ExecutionConfiguration executionConfig)
        {
            var dependencies = new Dictionary<Job, List<Job>>();

            using (var sqlConnection = new SqlConnection(executionConfig.ConnectionString))
            {
                using var sqlCommand = new SqlCommand(
                    @"SELECT
                    a.JobId,
                    b.JobName,
                    a.JobToExecuteId,
                    c.JobName as JobToExecuteName
                FROM etlmanager.Step AS a
                    INNER JOIN etlmanager.Job AS b ON a.JobId = b.JobId
                    INNER JOIN etlmanager.Job AS c ON a.JobToExecuteId = c.JobId
                WHERE a.StepType = 'JOB'"
                    , sqlConnection)
                {
                    CommandTimeout = 120 // two minutes
                };
                
                await sqlConnection.OpenAsync();
                using var reader = await sqlCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var jobId = reader["JobId"].ToString();
                    var jobName = reader["JobName"].ToString();
                    var jobToExecuteId = reader["JobToExecuteId"].ToString();
                    var jobToExecuteName = reader["JobToExecuteName"].ToString();

                    var job = new Job(jobId, jobName);
                    var jobToExecute = new Job(jobToExecuteId, jobToExecuteName);

                    if (!dependencies.ContainsKey(job))
                        dependencies[job] = new();

                    dependencies[job].Add(jobToExecute);
                }
            }

            List<List<Job>> cycles = dependencies.FindCycles();

            var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
            var json = JsonSerializer.Serialize(cycles, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });

            // There are no circular dependencies or this job is not among the cycles.
            return cycles.Count == 0 || !cycles.Any(jobs => jobs.Any(job => job.JobId == executionConfig.Job.JobId))
                ? null : json;
        }

    }
           
}
