using Dapper;
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

            string? encryptionPassword;
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
                try
                {
                    await UpdateExecutorProcessIdAsync(executionId, sqlConnection);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error updating process id for execution");
                }
                try
                {
                    (job, dependencyMode) = await GetExecutionDetailsAsync(executionId, sqlConnection);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting job details for execution");
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
            string? circularExecutions;
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

        private static async Task UpdateExecutorProcessIdAsync(string executionId, SqlConnection sqlConnection)
        {
            // Update this Executor process's PID for the execution.
            var process = Process.GetCurrentProcess();
            await sqlConnection.ExecuteAsync(
                "UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId",
                new { ProcessId = process.Id, ExecutionId = executionId });
        }

        private static async Task<(Job Job, bool DependencyMode)> GetExecutionDetailsAsync(string executionId, SqlConnection sqlConnection)
        {
            // Get execution details.
            (var dependencymode, var jobId, var jobName) = await sqlConnection.QueryFirstAsync<(bool, Guid, string)>(
                "SELECT TOP 1 DependencyMode, JobId, JobName FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId",
                new { ExecutionId = executionId });
            var job = new Job(jobId, jobName);
            return (job, dependencymode);
        }

        /// <summary>
        /// Checks for circular dependencies between jobs.
        /// Jobs can reference other jobs, so it's important to check them for circlular dependencies.
        /// </summary>
        /// <param name="executionConfig"></param>
        /// <returns>
        /// JSON string of circular job dependencies or null if there were no circular dependencies.
        /// </returns>
        private static async Task<string?> GetCircularJobExecutionsAsync(ExecutionConfiguration executionConfig)
        {
            var dependencies = await ReadDependenciesAsync(executionConfig.ConnectionString);
            List<List<Job>> cycles = dependencies.FindCycles();

            var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
            var json = JsonSerializer.Serialize(cycles, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });

            // There are no circular dependencies or this job is not among the cycles.
            return cycles.Count == 0 || !cycles.Any(jobs => jobs.Any(job => job.JobId == executionConfig.Job.JobId))
                ? null : json;
        }

        private static async Task<Dictionary<Job, List<Job>>> ReadDependenciesAsync(string connectionString)
        {
            using var sqlConnection = new SqlConnection(connectionString);
            var rows = (await sqlConnection.QueryAsync<(Guid, string, Guid, string)>(
                @"SELECT
                    a.JobId,
                    b.JobName,
                    a.JobToExecuteId,
                    c.JobName as JobToExecuteName
                FROM etlmanager.Step AS a
                    INNER JOIN etlmanager.Job AS b ON a.JobId = b.JobId
                    INNER JOIN etlmanager.Job AS c ON a.JobToExecuteId = c.JobId
                WHERE a.StepType = 'JOB'")).ToList();
            var jobs = rows.Select(row => (new Job(row.Item1, row.Item2), new Job(row.Item3, row.Item4))).ToList();
            var dependencies = jobs
                .GroupBy(key => key.Item1, element => element.Item2)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
            return dependencies;
        }

    }
           
}
