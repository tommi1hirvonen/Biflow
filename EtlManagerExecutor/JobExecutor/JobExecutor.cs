using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
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
                encryptionPassword = Utility.GetEncryptionKey(encryptionId, connectionString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting encryption password", executionId);
                return;
            }

            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            // Update this Executor process's PID for the execution.
            Process process = Process.GetCurrentProcess();
            SqlCommand processIdCmd = new SqlCommand(
                "UPDATE etlmanager.Execution SET ExecutorProcessId = @ProcessId WHERE ExecutionId = @ExecutionId", sqlConnection);
            processIdCmd.Parameters.AddWithValue("@ProcessId", process.Id);
            processIdCmd.Parameters.AddWithValue("@ExecutionId", executionId);
            processIdCmd.ExecuteNonQuery();

            // Get whether the execution should be run in dependency mode or in execution phase mode.
            SqlCommand dependencyModeCommand = new SqlCommand("SELECT TOP 1 DependencyMode FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            dependencyModeCommand.Parameters.AddWithValue("@ExecutionId", executionId);
            bool dependencyMode = (bool)dependencyModeCommand.ExecuteScalar();

            // Get the job id of the execution.
            SqlCommand jobIdCommand = new SqlCommand("SELECT TOP 1 JobId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            jobIdCommand.Parameters.AddWithValue("@ExecutionId", executionId);
            var jobId = jobIdCommand.ExecuteScalar().ToString();

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
                circularExecutions = GetCircularJobExecutions(executionConfig);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error checking for possible circular job executions", executionId);
                return;
            }

            if (!string.IsNullOrEmpty(circularExecutions))
            {
                UpdateErrorMessage(executionConfig, "Execution was cancelled because of circular job executions:\n" + circularExecutions);
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
            List<KeyValuePair<int, string>> allSteps = new();

            using SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            sqlConnection.Open();
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
                List<Task> stepWorkers = new();

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
                circularDependencies = GetCircularStepDependencies(executionConfig);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error checking for possible circular step dependencies", executionConfig.ExecutionId);
                return;
            }

            if (!string.IsNullOrEmpty(circularDependencies))
            {
                UpdateErrorMessage(executionConfig, "Execution was cancelled because of circular step dependencies:\n" + circularDependencies);
                Log.Error("{ExecutionId} Execution was cancelled because of circular step dependencies: " + circularDependencies, executionConfig.ExecutionId);
                return;
            }

            List<Task> stepWorkers = new();

            using (SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString))
            {
                sqlConnection.Open();

                // Get a list of all steps for this execution
                List<string> stepsToExecute = new();

                SqlCommand stepsListCommand = new SqlCommand("SELECT DISTINCT StepId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
                stepsListCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
                using (var reader = stepsListCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stepsToExecute.Add(reader[0].ToString());
                    }
                }


                while (stepsToExecute.Count > 0)
                {
                    List<string> stepsToSkip = new();
                    List<string> duplicateSteps = new();
                    List<string> executableSteps = new();

                    // Get a list of steps we can execute based on dependencies and already executed steps.

                    // Use a SQL command and the Execution to determine what to do with steps that have not yet been started.
                    SqlCommand stepActionCommand = new SqlCommand(
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

                    sqlConnection.OpenIfClosed();

                    // Iterate over the result rows and add steps to one of the following two lists either for skipping or for execution.
                    using (SqlDataReader reader = stepActionCommand.ExecuteReader())
                    {
                        while (reader.Read())
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
                        SqlCommand skipUpdateCommand = new SqlCommand(
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
                        skipUpdateCommand.ExecuteNonQuery();

                        stepsToExecute.Remove(stepId);

                        Log.Warning("{ExecutionId} {stepId} Marked step as SKIPPED", executionConfig.ExecutionId, stepId);
                    }

                    // Mark the steps that have a duplicate currently running under a different execution as DUPLICATE.
                    foreach (string stepId in duplicateSteps)
                    {
                        SqlCommand duplicateCommand = new SqlCommand(
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
                        duplicateCommand.ExecuteNonQuery();

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
            // Create a new step worker...
            StepWorker stepWorker = new StepWorker(executionConfig, stepId);
            //...and start it asynchronously.
            var task = stepWorker.ExecuteStepAsync();
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

        private static string GetCircularStepDependencies(ExecutionConfiguration executionConfig)
        {
            using SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            SqlCommand sqlCommand = new SqlCommand(
                    @"WITH Recursion AS (
                        SELECT
                            a.StepId,
                            a.DependantOnStepId,
                            CONVERT(NVARCHAR(MAX), b.StepName) AS DependencyPath
                        FROM etlmanager.Dependency AS a
                            INNER JOIN etlmanager.Step AS b ON a.StepId = b.StepId
                        WHERE b.JobId = @JobId
                        
                        UNION ALL
                        
                        SELECT
                            a.StepId,
                            b.DependantOnStepId,
                            DependencyPath = CONVERT(NVARCHAR(MAX), a.DependencyPath + ' => ' + c.StepName)
                        FROM Recursion AS a
                            INNER JOIN etlmanager.Dependency AS b ON a.DependantOnStepId = b.StepId
                            INNER JOIN etlmanager.Step AS c ON b.StepId = c.StepId
                        WHERE a.DependantOnStepId <> a.StepId AND b.DependantOnStepId <> b.StepId
                    )

                    SELECT
                        a.StepId,
                        a.DependantOnStepId,
                        DependencyPath = a.DependencyPath + ' => ' + b.StepName
                    FROM Recursion AS a
                        INNER JOIN etlmanager.Step AS b ON a.DependantOnStepId = b.StepId
                    WHERE a.StepId = a.DependantOnStepId
                    OPTION(MAXRECURSION 1000)"
                    , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            sqlCommand.Parameters.AddWithValue("@JobId", executionConfig.JobId);
            List<string> dependencyPaths = new();
            sqlConnection.OpenIfClosed();
            using (SqlDataReader reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    dependencyPaths.Add(reader["DependencyPath"].ToString());
                }
            }
            return string.Join("\n\n", dependencyPaths);
        }

        private static string GetCircularJobExecutions(ExecutionConfiguration executionConfig)
        {
            using SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            SqlCommand sqlCommand = new SqlCommand(
                @"WITH Recursion AS (
                    SELECT
                        a.JobId,
                        a.JobToExecuteId,
                        CONVERT(NVARCHAR(MAX), b.JobName) AS DependencyPath
                    FROM etlmanager.Step AS a
                        INNER JOIN etlmanager.Job AS b ON a.JobId = b.JobId
                    WHERE b.JobId = @JobId AND a.StepType = 'JOB'
                        
                    UNION ALL
                        
                    SELECT
                        a.JobId,
                        c.JobToExecuteId,
                        DependencyPath = CONVERT(NVARCHAR(MAX), a.DependencyPath + ' => ' + b.JobName)
                    FROM Recursion AS a
                        INNER JOIN etlmanager.Job AS b ON a.JobToExecuteId = b.JobId
                        INNER JOIN etlmanager.Step AS c ON b.JobId = c.JobId AND c.StepType = 'JOB'
                    WHERE a.JobToExecuteId <> a.JobId
                )
                SELECT
                    a.JobId,
                    a.JobToExecuteId,
                    DependencyPath = a.DependencyPath + ' => ' + b.JobName
                FROM Recursion AS a
                    INNER JOIN etlmanager.Job AS b ON a.JobToExecuteId = b.JobId
                WHERE a.JobId = a.JobToExecuteId
                OPTION(MAXRECURSION 1000)"
                , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            sqlCommand.Parameters.AddWithValue("@JobId", executionConfig.JobId);
            List<string> dependencyPaths = new();
            sqlConnection.OpenIfClosed();
            using (SqlDataReader reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    dependencyPaths.Add(reader["DependencyPath"].ToString());
                }
            }
            return string.Join("\n\n", dependencyPaths);
        }

        private static void UpdateErrorMessage(ExecutionConfiguration executionConfig, string errorMessage)
        {
            using SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString);
            SqlCommand sqlCommand = new SqlCommand(
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
                sqlConnection.OpenIfClosed();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error updating error message", executionConfig.ExecutionId);
            }
        }
    }
           
}
