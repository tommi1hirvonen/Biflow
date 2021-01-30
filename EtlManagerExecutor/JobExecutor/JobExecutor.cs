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

        enum ExecutionStatus
        {
            NotStarted,
            Running,
            Success,
            Failed
        };

        private Dictionary<string, ExecutionStatus> StepStatuses { get; set; } = new();

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
                connectionString: connectionString,
                encryptionKey: encryptionPassword,
                maxParallelSteps: maxParallelSteps,
                pollingIntervalMs: pollingIntervalMs,
                executionId: executionId,
                jobId: jobId,
                notify: notify,
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
            if (notify && StepStatuses.Any(status => status.Value == ExecutionStatus.Failed))
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
            // List of steps (id), their dependency steps (id) and whether it's a strict dependency or not.
            Dictionary<string, HashSet<KeyValuePair<string, bool>>> stepDependencies;
            try
            {
                Dictionary<Step, List<KeyValuePair<Step, bool>>> dependencies = await GetCircularStepDependenciesAsync(executionConfig);
                stepDependencies = dependencies.ToDictionary(
                    pair => pair.Key.StepId,
                    pair => pair.Value.Select(dependency => new KeyValuePair<string, bool>(dependency.Key.StepId, dependency.Value)
                    ).ToHashSet());
                List<List<Step>> cycles = dependencies.ToDictionary(pair => pair.Key, pair => pair.Value.Select(dep => dep.Key)).FindCycles();

                // If there are circular dependencies, update error message for all steps and cancel execution.
                if (cycles.Count > 0)
                {
                    var encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
                    var json = JsonSerializer.Serialize(cycles, new JsonSerializerOptions { WriteIndented = true, Encoder = encoder });
                    await UpdateErrorMessageAsync(executionConfig, "Execution was cancelled because of circular step dependencies:\n" + json);
                    Log.Error("{ExecutionId} Execution was cancelled because of circular step dependencies: " + json, executionConfig.ExecutionId);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error checking for possible circular step dependencies", executionConfig.ExecutionId);
                return;
            }



            var stepWorkers = new List<Task>();

            using (var sqlConnection = new SqlConnection(executionConfig.ConnectionString))
            {
                await sqlConnection.OpenAsync();

                // Get steps to execute and add them to the list of steps and their statuses.
                var stepsListCommand = new SqlCommand("SELECT DISTINCT StepId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
                stepsListCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
                using (var reader = await stepsListCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var stepId = reader[0].ToString();
                        StepStatuses[stepId] = ExecutionStatus.NotStarted;
                    }
                }

                while (StepStatuses.Any(step => step.Value == ExecutionStatus.NotStarted))
                {
                    IEnumerable<string> stepsToExecute = StepStatuses.Where(step => step.Value == ExecutionStatus.NotStarted).Select(step => step.Key);
                    var stepsToSkip = new List<string>();
                    var executableSteps = new List<string>();

                    foreach (var stepId in stepsToExecute)
                    {
                        // Step has dependencies
                        if (stepDependencies.ContainsKey(stepId))
                        {
                            HashSet<KeyValuePair<string, bool>> dependencies = stepDependencies[stepId];
                            IEnumerable<string> strictDependencies = dependencies.Where(dep => dep.Value).Select(dep => dep.Key);
                            
                            // If there are any strict dependencies, which have been marked as failed, skip this step.
                            if (strictDependencies.Any(dep => StepStatuses.Any(status => status.Value == ExecutionStatus.Failed && status.Key == dep)))
                            {
                                stepsToSkip.Add(stepId);
                            }
                            // If the steps dependencies have been completed (success/failure), the step can be executed.
                            // Also check if the dependency is actually included in the execution.
                            else if (dependencies.All(dep => !StepStatuses.ContainsKey(dep.Key) ||
                            StepStatuses[dep.Key] == ExecutionStatus.Success || StepStatuses[dep.Key] == ExecutionStatus.Failed))
                            {
                                executableSteps.Add(stepId);
                            }
                            // Otherwise wait until the step can be executed.
                        }
                        else
                        {
                            // Step has no dependencies. It can be executed.
                            executableSteps.Add(stepId);
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

                        StepStatuses[stepId] = ExecutionStatus.Failed;

                        Log.Warning("{ExecutionId} {stepId} Marked step as SKIPPED", executionConfig.ExecutionId, stepId);
                    }

                    foreach (string stepId in executableSteps)
                    {
                        // Check whether the maximum number of parallel steps are running
                        // and wait for some steps to finish if necessary.
                        while (RunningStepsCounter >= executionConfig.MaxParallelSteps)
                        {
                            await Task.Delay(executionConfig.PollingIntervalMs);
                        }

                        StepStatuses[stepId] = ExecutionStatus.Running;
                        stepWorkers.Add(StartNewStepWorkerAsync(executionConfig, stepId));

                        Log.Information("{ExecutionId} {stepId} Started step execution", executionConfig.ExecutionId, stepId);
                    }

                    // Wait before doing another progress and dependencies check.
                    // This way we aren't constantly looping and querying the status.
                    if (StepStatuses.Any(step => step.Value == ExecutionStatus.NotStarted))
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

            bool result = false;
            try
            {
                // Wait for the step to finish.
                result = await task;
            }
            finally
            {
                // Update the status.
                StepStatuses[stepId] = result ? ExecutionStatus.Success : ExecutionStatus.Failed;

                // Subtract one from the counter.
                Interlocked.Decrement(ref RunningStepsCounter);
                Log.Information("{ExecutionId} {StepId} Finished step execution", executionConfig.ExecutionId, stepId);
            }
        }

        // Returns a list of steps, its dependency steps and whether it's a strict dependency or not.
        private static async Task<Dictionary<Step, List<KeyValuePair<Step, bool>>>> GetCircularStepDependenciesAsync(ExecutionConfiguration executionConfig)
        {
            using var sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            // Get a list of dependencies for this execution. Only include steps selected for execution in the check.
            var sqlCommand = new SqlCommand(
                    @"SELECT
                        a.StepId,
                        b.StepName,
                        a.DependantOnStepId,
                        c.StepName as DependantOnStepName,
                        a.StrictDependency
                    FROM etlmanager.Dependency AS a
                        INNER JOIN etlmanager.Step AS b ON a.StepId = b.StepId
                        INNER JOIN etlmanager.Step AS c ON a.DependantOnStepId = c.StepId
                        INNER JOIN etlmanager.Execution AS d ON a.StepId = d.StepId AND d.ExecutionId = @ExecutionId AND d.RetryAttemptIndex = 0
                        INNER JOIN etlmanager.Execution AS e ON a.DependantOnStepId = e.StepId AND e.ExecutionId = @ExecutionId AND e.RetryAttemptIndex = 0
                    WHERE b.JobId = @JobId"
                    , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            sqlCommand.Parameters.AddWithValue("@JobId", executionConfig.JobId);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
            var dependencies = new Dictionary<Step, List<KeyValuePair<Step, bool>>>();
            await sqlConnection.OpenAsync();
            using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var stepId = reader["StepId"].ToString();
                    var stepName = reader["StepName"].ToString();
                    var dependencyStepId = reader["DependantOnStepId"].ToString();
                    var dependencyStepName = reader["DependantOnStepName"].ToString();
                    var strict = (bool)reader["StrictDependency"];

                    var step = new Step(stepId, stepName);
                    var dependencyStep = new Step(dependencyStepId, dependencyStepName);

                    if (!dependencies.ContainsKey(step))
                        dependencies[step] = new();
                    
                    dependencies[step].Add(new(dependencyStep, strict));
                }
            }

            return dependencies;
        }

        private static async Task<string> GetCircularJobExecutionsAsync(ExecutionConfiguration executionConfig)
        {
            using var sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            var sqlCommand = new SqlCommand(
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
            var dependencies = new Dictionary<Job, List<Job>>();
            await sqlConnection.OpenAsync();
            using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
            {
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
            return cycles.Count == 0 || !cycles.Any(jobs => jobs.Any(job => job.JobId == executionConfig.JobId))
                ? null : json;
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
