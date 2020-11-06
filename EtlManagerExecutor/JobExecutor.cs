using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace EtlManagerExecutor
{
    class JobExecutor : IJobExecutor
    {
        private readonly ILogger<JobExecutor> logger;
        private readonly IConfiguration configuration;

        private string EtlManagerConnectionString { get; set; }
        private int PollingIntervalMs { get; set; }
        private int MaximumParallelSteps { get; set; }
        private string EncryptionPassword { get; set; }

        private string ExecutionId { get; set; }
        private string JobId { get; set; }

        private bool Notify { get; set; } = false;

        private int RunningStepsCounter { get; set; } = 0;

        public JobExecutor(ILogger<JobExecutor> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public void Run(string executionId, bool notify, string encryptionKey)
        {
            EtlManagerConnectionString = configuration.GetValue<string>("EtlManagerConnectionString");
            PollingIntervalMs = configuration.GetValue<int>("PollingIntervalMs");
            MaximumParallelSteps = configuration.GetValue<int>("MaximumParallelSteps");

            // If the encryption key was provided, use that. Otherwise try to read it from the database.
            if (encryptionKey != null)
            {
                EncryptionPassword = encryptionKey;
            }
            else
            {
                EncryptionPassword = Utility.GetEncryptionKey(EtlManagerConnectionString);
            }
            

            ExecutionId = executionId;

            Notify = notify;

            using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
            sqlConnection.Open();

            SqlCommand dependencyModeCommand = new SqlCommand("SELECT TOP 1 DependencyMode FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            dependencyModeCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
            bool dependencyMode = (bool)dependencyModeCommand.ExecuteScalar();

            SqlCommand jobIdCommand = new SqlCommand("SELECT TOP 1 JobId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            jobIdCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
            JobId = jobIdCommand.ExecuteScalar().ToString();

            string circularExecutions;
            try
            {
                circularExecutions = GetCircularJobExecutions();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ExecutionId} Error checking for possible circular job executions", ExecutionId);
                return;
            }

            if (circularExecutions != null && circularExecutions.Length > 0)
            {
                UpdateErrorMessage("Execution was cancelled because of circular job executions:\n" + circularExecutions);
                logger.LogError("{ExecutionId} Execution was cancelled because of circular job executions: " + circularExecutions, ExecutionId);
                return;
            }

            if (dependencyMode)
            {
                logger.LogInformation("{ExecutionId} Starting execution in dependency mode", ExecutionId);
                ExecuteInDependencyMode();
            }
            else
            {
                logger.LogInformation("{ExecutionId} Starting execution in execution phase mode", ExecutionId);
                ExecuteInExecutionPhaseMode();
            }

            // Execution finished. Notify subscribers of possible errors.
            if (Notify)
            {
                EmailHelper.SendNotification(configuration, ExecutionId);
            }
        }

        private void ExecuteInExecutionPhaseMode()
        {
            List<KeyValuePair<int, string>> allSteps = new List<KeyValuePair<int, string>>();

            using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
            sqlConnection.Open();
            SqlCommand sqlCommand = new SqlCommand("SELECT DISTINCT StepId, ExecutionPhase FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
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

                foreach (string stepId in stepsToExecute)
                {
                    // Check whether the maximum number of parallel steps are running
                    // and wait for some steps to finish if necessary.
                    while (RunningStepsCounter >= MaximumParallelSteps)
                    {
                        Thread.Sleep(PollingIntervalMs);
                    }

                    StartNewStepWorker(stepId);

                    logger.LogInformation("{ExecutionId} Started step worker for step {stepId}", ExecutionId, stepId);

                }

                // All steps have been started. Poll until the counter is zero => all steps have been completed.
                while (RunningStepsCounter > 0)
                {
                    Thread.Sleep(PollingIntervalMs);
                }
            }
            
        }

        private void ExecuteInDependencyMode()
        {
            string circularDependencies;
            try
            {
                circularDependencies = GetCircularStepDependencies();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ExecutionId} Error checking for possible circular step dependencies", ExecutionId);
                return;
            }

            if (circularDependencies != null && circularDependencies.Length > 0)
            {
                UpdateErrorMessage("Execution was cancelled because of circular step dependencies:\n" + circularDependencies);
                logger.LogError("{ExecutionId} Execution was cancelled because of circular step dependencies: " + circularDependencies, ExecutionId);
                return;
            }

            using (SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString))
            {
                sqlConnection.Open();

                // Get a list of all steps for this execution
                List<string> stepsToExecute = new List<string>();

                SqlCommand stepsListCommand = new SqlCommand("SELECT DISTINCT StepId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
                stepsListCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                using (var reader = stepsListCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stepsToExecute.Add(reader[0].ToString());
                    }
                }


                while (stepsToExecute.Count > 0)
                {
                    List<string> stepsToSkip = new List<string>();
                    List<string> duplicateSteps = new List<string>();
                    List<string> executableSteps = new List<string>();

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
                    stepActionCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);

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
                        skipUpdateCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                        skipUpdateCommand.Parameters.AddWithValue("@StepId", stepId);
                        skipUpdateCommand.ExecuteNonQuery();

                        stepsToExecute.Remove(stepId);

                        logger.LogInformation("{ExecutionId} Marked step {stepId} as SKIPPED", ExecutionId, stepId);
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
                        duplicateCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                        duplicateCommand.Parameters.AddWithValue("@StepId", stepId);
                        duplicateCommand.ExecuteNonQuery();

                        stepsToExecute.Remove(stepId);

                        logger.LogInformation("{ExecutionId} Marked step {stepId} as DUPLICATE", ExecutionId, stepId);

                    }

                    foreach (string stepId in executableSteps)
                    {
                        // Check whether the maximum number of parallel steps are running
                        // and wait for some steps to finish if necessary.
                        while (RunningStepsCounter >= MaximumParallelSteps)
                        {
                            Thread.Sleep(PollingIntervalMs);
                        }

                        StartNewStepWorker(stepId);

                        stepsToExecute.Remove(stepId);

                        logger.LogInformation("{ExecutionId} Started execution for step {stepId}", ExecutionId, stepId);

                    }

                }

            }

            // All steps have been started. Poll until the counter is zero => all steps have been completed.
            while (RunningStepsCounter > 0)
            {
                Thread.Sleep(PollingIntervalMs);
            }
        }

        private void StartNewStepWorker(string stepId)
        {
            StepWorker stepWorker = new StepWorker(ExecutionId, stepId, EtlManagerConnectionString, PollingIntervalMs, Notify, OnStepCompleted, EncryptionPassword);
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(stepWorker.OnStepCompleted);
            backgroundWorker.DoWork += new DoWorkEventHandler(stepWorker.ExecuteStep);
            backgroundWorker.RunWorkerAsync();
            RunningStepsCounter++;
        }

        void OnStepCompleted(string stepId)
        {
            RunningStepsCounter--;

            logger.LogInformation("{ExecutionId} Finished executing step {StepId}", ExecutionId, stepId);
        }

        private string GetCircularStepDependencies()
        {
            using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
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
            sqlCommand.Parameters.AddWithValue("@JobId", JobId);
            List<string> dependencyPaths = new List<string>();
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

        private string GetCircularJobExecutions()
        {
            using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
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
            sqlCommand.Parameters.AddWithValue("@JobId", JobId);
            List<string> dependencyPaths = new List<string>();
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

        private void UpdateErrorMessage(string errorMessage)
        {
            using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
            SqlCommand sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET ExecutionStatus = 'FAILED', ErrorMessage = @ErrorMessage, StartDateTime = GETDATE(), EndDateTime = GETDATE()
                    WHERE ExecutionId = @ExecutionId"
                    , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            sqlCommand.Parameters.AddWithValue("@ErrorMessage", errorMessage);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
            try
            {
                sqlConnection.OpenIfClosed();
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ExecutionId} Error updating error message", ExecutionId);
            }
        }
    }
}
