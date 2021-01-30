using Microsoft.Azure.Management.DataFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class ExecutionStopper : IExecutionStopper
    {
        private readonly IConfiguration configuration;
        public ExecutionStopper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<bool> RunAsync(string executionId, string username)
        {
            var connectionString = configuration.GetValue<string>("EtlManagerConnectionString");
            var encryptionId = configuration.GetValue<string>("EncryptionId");

            // First stop the EtlManagerExecutor process. This stops SQL executions as well.
            try
            {
                if (!await StopExecutorProcessAsync(connectionString, executionId))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error stopping Executor process", executionId);
                return false;
            }

            // Log SQL steps as STOPPED.
            Log.Information("{executionId} Logging SQL steps as stopped", executionId);
            try
            {
                using var sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();
                SqlCommand updateStatuses = new SqlCommand(
                  @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(),
                    StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                ExecutionStatus = 'STOPPED',
                    StoppedBy = @Username
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND StepType = 'SQL'"
                    , sqlConnection);
                updateStatuses.Parameters.AddWithValue("@ExecutionId", executionId);
                if (username != null) updateStatuses.Parameters.AddWithValue("@Username", username);
                else updateStatuses.Parameters.AddWithValue("@Username", DBNull.Value);
                await updateStatuses.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error logging SQL steps as stopped", executionId);
            }

            // Get encryption key for package and pipeline stopping.
            string encryptionKey;
            try
            {
                encryptionKey = await Utility.GetEncryptionKeyAsync(encryptionId, connectionString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting encryption key to stop packages and pipelines", executionId);
                return false;
            }

            var stopConfiguration = new ConfigurationBase(connectionString, executionId, encryptionKey, username);            
            
            return await StopStepExecutionsAsync(stopConfiguration);
        }

        private static async Task<bool> StopExecutorProcessAsync(string connectionString, string executionId)
        {
            int executorProcessId;

            try
            {
                // Get the process id for the execution.
                using var sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();

                var fetchProcessId = new SqlCommand(
                    "SELECT TOP 1 ExecutorProcessId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId"
                    , sqlConnection);
                fetchProcessId.Parameters.AddWithValue("@ExecutionId", executionId);
                var result = await fetchProcessId.ExecuteScalarAsync();
                if (result is null)
                {
                    Log.Warning("{executionId} No Executor process id for given execution id. Execution stopping canceled.", executionId);
                    return false;
                }
                executorProcessId = (int)result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting Executor process id", executionId);
                return false;
            }

            // Get the process and check that its name matches so that we don't accidentally kill the wrong process.
            Process executorProcess;

            try
            {
                executorProcess = Process.GetProcessById(executorProcessId);
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "{executionId} Error stopping Executor process. The process id {executorProcessId} is not running", executionId, executorProcessId);
                return false;
            }

            string processName = executorProcess.ProcessName;
            if (!processName.Equals("EtlManagerExecutor"))
            {
                Log.Warning("{executionId} Process id {executorProcessId} does not map to an instance of EtlManagerExecutor", executionId, executorProcessId);
                return false;
            }

            Log.Information("{executionId} Killing Executor process id {executorProcessId}", executionId, executorProcessId);

            executorProcess.Kill();
            executorProcess.WaitForExit();

            return true;
        }

        private static async Task<bool> StopStepExecutionsAsync(ConfigurationBase configuration)
        {
            var steps = new List<ICancelable>();
            var cancellationTasks = new List<Task<bool>>();

            Log.Information("{ExecutionId} Getting package and pipeline executions to be stopped", configuration.ExecutionId);
            try
            {
                using var sqlConnection = new SqlConnection(configuration.ConnectionString);
                await sqlConnection.OpenAsync();

                var fetchStepDetails = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, StepType,
                        PackageOperationId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionKey) AS ConnectionString,
                        PipelineRunId, DataFactoryId
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND (PackageOperationId IS NOT NULL OR PipelineRunId IS NOT NULL)"
                    , sqlConnection);
                fetchStepDetails.Parameters.AddWithValue("@ExecutionId", configuration.ExecutionId);
                fetchStepDetails.Parameters.AddWithValue("@EncryptionKey", configuration.EncryptionKey);

                using var stepReader = await fetchStepDetails.ExecuteReaderAsync();
                while (await stepReader.ReadAsync())
                {
                    string stepId = stepReader["StepId"].ToString();
                    int retryAttemptIndex = (int)stepReader["RetryAttemptIndex"];
                    string stepType = stepReader["StepType"].ToString();
                    if (stepType == "SSIS")
                    {
                        long packageOperationId = (long)stepReader["PackageOperationId"];
                        string packageConnectionString = stepReader["ConnectionString"].ToString();
                        var packageStep = new PackageStep(configuration, stepId, packageConnectionString)
                        {
                            RetryAttemptCounter = retryAttemptIndex,
                            PackageOperationId = packageOperationId
                        };
                        steps.Add(packageStep);
                    }
                    else if (stepType == "PIPELINE")
                    {
                        string runId = stepReader["PipelineRunId"].ToString();
                        string dataFactoryId = stepReader["DataFactoryId"].ToString();
                        var pipelineStep = new PipelineStep(configuration, stepId, dataFactoryId)
                        {
                            RetryAttemptCounter = retryAttemptIndex,
                            PipelineRunId = runId
                        };
                        steps.Add(pipelineStep);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error reading package and pipeline executions", configuration.ExecutionId);
            }

            foreach (var step in steps)
            {
                cancellationTasks.Add(step.CancelAsync());
            }

            bool[] results = await Task.WhenAll(cancellationTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

    }

}
