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

        private int RunningStepsCounter = 0;

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
                encryptionPassword = await Utility.GetEncryptionKeyAsync(encryptionId, connectionString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting encryption password", executionId);
                return;
            }

            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();

            // Update this Executor process's PID for the execution.
            var process = Process.GetCurrentProcess();
            var processIdCmd = new SqlCommand("UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection);
            processIdCmd.Parameters.AddWithValue("@ProcessId", process.Id);
            processIdCmd.Parameters.AddWithValue("@ExecutionId", executionId);
            await processIdCmd.ExecuteNonQueryAsync();

            // Get whether the execution should be run in dependency mode or in execution phase mode.
            var dependencyModeCommand = new SqlCommand("SELECT TOP 1 DependencyMode FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            dependencyModeCommand.Parameters.AddWithValue("@ExecutionId", executionId);
            var dependencyMode = (bool)await dependencyModeCommand.ExecuteScalarAsync();

            // Get the job id of the execution.
            var jobIdCommand = new SqlCommand("SELECT TOP 1 JobId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            jobIdCommand.Parameters.AddWithValue("@ExecutionId", executionId);
            var jobId = (await jobIdCommand.ExecuteScalarAsync()).ToString();

            var executionConfig = new ExecutionConfiguration(
                connectionString,
                encryptionPassword,
                maxParallelSteps,
                pollingIntervalMs,
                executionId,
                jobId,
                notify);

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
                await UpdateErrorMessageAsync(executionConfig, "Execution was cancelled because of circular job executions:\n" + circularExecutions);
                Log.Error("{executionId} Execution was cancelled because of circular job executions: " + circularExecutions, executionId);
                return;
            }

            
            if (dependencyMode)
            {
                Log.Information("{ExecutionId} Starting execution in dependency mode", executionId);
                await ExecuteInDependencyMode(executionConfig);
            }
            else
            {
                Log.Information("{executionId} Starting execution in execution phase mode", executionId);
                await ExecuteInExecutionPhaseMode(executionConfig);
            }

            // Execution finished. Notify subscribers of possible errors.
            if (notify)
            {
                EmailHelper.SendNotification(configuration, executionId);
            }
        }

        private async Task ExecuteInExecutionPhaseMode(ExecutionConfiguration executionConfig)
        {
            var allSteps = new List<KeyValuePair<int, string>>();

            using var sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            await sqlConnection.OpenAsync();
            SqlCommand sqlCommand = new SqlCommand("SELECT DISTINCT StepId, ExecutionPhase FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
            using (var reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    var step = new KeyValuePair<int, string>((int)reader["ExecutionPhase"], reader["StepId"].ToString());
                    allSteps.Add(step);
                }
            }

            List<int> executionPhases = allSteps.Select(step => step.Key).Distinct().ToList();
            executionPhases.Sort();

            foreach (int executionPhase in executionPhases)
            {
                List<string> stepsToExecute = allSteps.Where(step => step.Key == executionPhase).Select(step => step.Value).ToList();
                var stepWorkers = new List<Task>();

                foreach (string stepId in stepsToExecute)
                {
                    // Check whether the maximum number of parallel steps are running
                    // and wait for some steps to finish if necessary.
                    while (RunningStepsCounter >= executionConfig.MaxParallelSteps)
                    {
                        await Task.Delay(executionConfig.PollingIntervalMs);
                    }

                    stepWorkers.Add(StartNewStepWorkerAsync(executionConfig, stepId));

                    Log.Information("{ExecutionId} {stepId} Started step worker", executionConfig.ExecutionId, stepId);

                }

                // All steps have been started. Wait until all step worker tasks have finished.
                await Task.WhenAll(stepWorkers);
            }
            
        }

        private async Task ExecuteInDependencyMode(ExecutionConfiguration executionConfig)
        {
            string circularDependencies;
            try
            {
                circularDependencies = await GetCircularStepDependenciesAsync(executionConfig);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error checking for possible circular step dependencies", executionConfig.ExecutionId);
                return;
            }

            if (!string.IsNullOrEmpty(circularDependencies))
            {
                await UpdateErrorMessageAsync(executionConfig, "Execution was cancelled because of circular step dependencies:\n" + circularDependencies);
                Log.Error("{ExecutionId} Execution was cancelled because of circular step dependencies: " + circularDependencies, executionConfig.ExecutionId);
                return;
            }

            var stepWorkers = new List<Task>();

            using (var sqlConnection = new SqlConnection(executionConfig.ConnectionString))
            {
                await sqlConnection.OpenAsync();

                // Get a list of all steps for this execution
                var stepsToExecute = new List<string>();

                var stepsListCommand = new SqlCommand("SELECT DISTINCT StepId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
                stepsListCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
                using (var reader = await stepsListCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stepsToExecute.Add(reader[0].ToString());
                    }
                }


                while (stepsToExecute.Count > 0)
                {
                    var stepsToSkip = new List<string>();
                    var duplicateSteps = new List<string>();
                    var executableSteps = new List<string>();

                    // Get a list of steps we can execute based on dependencies and already executed steps.

                    // Use a SQL command and the Execution to determine what to do with steps that have not yet been started.
                    var stepActionCommand = new SqlCommand(
                        @"SELECT a.StepId,
                        CASE WHEN EXISTS (" + // There are strict dependencies which have been stopped, skipped or which have failed => step should be skipped
                                @"SELECT *
                                FROM etlmanager.Dependency AS x
                                    INNER JOIN etlmanager.Execution AS y ON x.DependantOnStepId = y.StepId AND y.ExecutionId = b.ExecutionId
                                WHERE x.StepId = a.StepId AND x.StrictDependency = 1 AND y.ExecutionStatus IN ('FAILED', 'SKIPPED', 'STOPPED')
                            ) THEN '1'
                            ELSE '0'
                        END AS Skip,
                        CASE WHEN EXISTS (" + // The same step is running under a different execution => step should be skipped and marked as duplicate
                                @"SELECT *
                                FROM etlmanager.Execution AS x
                                WHERE b.StepId = x.StepId AND x.ExecutionStatus = 'RUNNING'
                            ) THEN '1'
                            ELSE '0'
                        END AS Duplicate,
                        CASE WHEN NOT EXISTS (" + // There are no dependencies which have not been started, are not running or are not awaiting a retry => step can be started
                                @"SELECT *
                                FROM etlmanager.Dependency AS x
                                    INNER JOIN etlmanager.Execution AS y ON x.DependantOnStepId = y.StepId AND y.ExecutionId = b.ExecutionId
                                WHERE x.StepId = a.StepId AND y.ExecutionStatus IN ('NOT STARTED', 'RUNNING', 'AWAIT RETRY')
                            ) AND NOT EXISTS (" + // Also double check skip logic because in very fast environments steps might get executed even though they should be skipped.
                                @"SELECT *
                                FROM etlmanager.Dependency AS x
                                    INNER JOIN etlmanager.Execution AS y ON x.DependantOnStepId = y.StepId AND y.ExecutionId = b.ExecutionId
                                WHERE x.StepId = a.StepId AND x.StrictDependency = 1 AND y.ExecutionStatus IN ('FAILED', 'SKIPPED', 'STOPPED')
                            )
                            THEN '1'
                            ELSE '0'
                        END AS Executable
                    FROM etlmanager.Step AS a
                        INNER JOIN etlmanager.Execution AS b ON b.ExecutionId = @ExecutionId AND a.StepId = b.StepId
                    WHERE b.ExecutionStatus = 'NOT STARTED'"
                        , sqlConnection)
                    {
                        CommandTimeout = 120 // two minutes
                    };
                    stepActionCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);

                    await sqlConnection.OpenIfClosedAsync();

                    // Iterate over the result rows and add steps to one of the following two lists either for skipping or for execution.
                    using (var reader = await stepActionCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string stepId = reader["StepId"].ToString();
                            string skip = reader["Skip"].ToString();
                            string duplicate = reader["Duplicate"].ToString();
                            string executable = reader["Executable"].ToString();

                            if (!stepsToExecute.Contains(stepId)) continue;
                            else if (skip == "1") stepsToSkip.Add(stepId);
                            else if (duplicate == "1") duplicateSteps.Add(stepId);
                            else if (executable == "1") executableSteps.Add(stepId);
                        }
                    }

                    // Mark the steps that should be skipped in the execution table as SKIPPED.
                    foreach (string stepId in stepsToSkip)
                    {
                        var skipUpdateCommand = new SqlCommand(
                                        @"UPDATE etlmanager.Execution
                                    SET ExecutionStatus = 'SKIPPED',
                                    StartDateTime = GETDATE(), EndDateTime = GETDATE()
                                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                                        , sqlConnection)
                        {
                            CommandTimeout = 120 // two minutes
                        };
                        skipUpdateCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
                        skipUpdateCommand.Parameters.AddWithValue("@StepId", stepId);
                        await skipUpdateCommand.ExecuteNonQueryAsync();

                        stepsToExecute.Remove(stepId);

                        Log.Warning("{ExecutionId} {stepId} Marked step as SKIPPED", executionConfig.ExecutionId, stepId);
                    }

                    // Mark the steps that have a duplicate currently running under a different execution as DUPLICATE.
                    foreach (string stepId in duplicateSteps)
                    {
                        var duplicateCommand = new SqlCommand(
                                        @"UPDATE etlmanager.Execution
                                    SET ExecutionStatus = 'DUPLICATE',
                                    StartDateTime = GETDATE(), EndDateTime = GETDATE()
                                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                                        , sqlConnection)
                        {
                            CommandTimeout = 120 // two minutes
                        };
                        duplicateCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
                        duplicateCommand.Parameters.AddWithValue("@StepId", stepId);
                        await duplicateCommand.ExecuteNonQueryAsync();

                        stepsToExecute.Remove(stepId);

                        Log.Warning("{ExecutionId} {stepId} Marked step as DUPLICATE", executionConfig.ExecutionId, stepId);

                    }

                    foreach (string stepId in executableSteps)
                    {
                        // Check whether the maximum number of parallel steps are running
                        // and wait for some steps to finish if necessary.
                        while (RunningStepsCounter >= executionConfig.MaxParallelSteps)
                        {
                            await Task.Delay(executionConfig.PollingIntervalMs);
                        }

                        stepWorkers.Add(StartNewStepWorkerAsync(executionConfig, stepId));

                        stepsToExecute.Remove(stepId);

                        Log.Information("{ExecutionId} {stepId} Started step execution", executionConfig.ExecutionId, stepId);

                    }

                    // Wait before doing another progress and dependencies check.
                    // This way we aren't constantly looping and querying the status.
                    if (stepsToExecute.Count > 0)
                    {
                        await Task.Delay(executionConfig.PollingIntervalMs);
                    }

                }

            }

            // All steps have been started. Wait until all step worker tasks have finished.
            await Task.WhenAll(stepWorkers);
        }

        private async Task StartNewStepWorkerAsync(ExecutionConfiguration executionConfig, string stepId)
        {
            // Create a new step worker and start it asynchronously.
            var task = new StepWorker(executionConfig, stepId).ExecuteStepAsync();
            // Add one to the counter.
            Interlocked.Increment(ref RunningStepsCounter);
            try
            {
                // Wait for the step to finish.
                await task;
            }
            finally
            {
                // Subtract one from the counter.
                Interlocked.Decrement(ref RunningStepsCounter);
                Log.Information("{ExecutionId} {StepId} Finished step execution", executionConfig.ExecutionId, stepId);
            }
        }

        private static async Task<string> GetCircularStepDependenciesAsync(ExecutionConfiguration executionConfig)
        {
            using var sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            var sqlCommand = new SqlCommand(
                    @"SELECT
                        a.StepId,
                        a.DependantOnStepId
                    FROM etlmanager.Dependency AS a
                        INNER JOIN etlmanager.Step AS b ON a.StepId = b.StepId
                    WHERE b.JobId = @JobId"
                    , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            sqlCommand.Parameters.AddWithValue("@JobId", executionConfig.JobId);
            var dependencies = new Dictionary<string, List<string>>();
            await sqlConnection.OpenAsync();
            using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var stepId = reader["StepId"].ToString();
                    var dependencyStepId = reader["DependantOnStepId"].ToString();
                    if (!dependencies.ContainsKey(stepId))
                    {
                        dependencies[stepId] = new();
                    }
                    dependencies[stepId].Add(dependencyStepId);
                }
            }

            List<List<string>> cycles = dependencies.FindCycles();

            return cycles.Count == 0 ? null : JsonSerializer.Serialize(cycles, new JsonSerializerOptions { WriteIndented = true });
        }

        private static async Task<string> GetCircularJobExecutionsAsync(ExecutionConfiguration executionConfig)
        {
            using var sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            var sqlCommand = new SqlCommand(
                @"SELECT
                    a.JobId,
                    a.JobToExecuteId
                FROM etlmanager.Step AS a
                    INNER JOIN etlmanager.Job AS b ON a.JobId = b.JobId
                WHERE a.StepType = 'JOB'"
                , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            var dependencies = new Dictionary<string, List<string>>();
            await sqlConnection.OpenAsync();
            using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var jobId = reader["JobId"].ToString();
                    var jobToExecuteId = reader["JobToExecuteId"].ToString();
                    if (!dependencies.ContainsKey(jobId))
                    {
                        dependencies[jobId] = new();
                    }
                    dependencies[jobId].Add(jobToExecuteId);
                }
            }

            List<List<string>> cycles = dependencies.FindCycles();

            // There are no circular dependencies or this job is not among the cycles.
            return cycles.Count == 0 || !cycles.Any(c => c.Any(c_ => c_ == executionConfig.JobId))
                ? null : JsonSerializer.Serialize(cycles, new JsonSerializerOptions { WriteIndented = true });
        }

        private static async Task UpdateErrorMessageAsync(ExecutionConfiguration executionConfig, string errorMessage)
        {
            using var sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            var sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET ExecutionStatus = 'FAILED', ErrorMessage = @ErrorMessage, StartDateTime = GETDATE(), EndDateTime = GETDATE()
                    WHERE ExecutionId = @ExecutionId"
                    , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            sqlCommand.Parameters.AddWithValue("@ErrorMessage", errorMessage);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
            try
            {
                await sqlConnection.OpenAsync();
                await sqlCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error updating error message", executionConfig.ExecutionId);
            }
        }
    }
           
}
