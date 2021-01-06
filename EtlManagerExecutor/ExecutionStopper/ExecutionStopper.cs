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
    partial class ExecutionStopper : IExecutionStopper
    {
        private readonly IConfiguration configuration;
        public ExecutionStopper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<bool> RunAsync(string executionId, string username)
        {
            string connectionString = configuration.GetValue<string>("EtlManagerConnectionString");
            string encryptionId = configuration.GetValue<string>("EncryptionId");

            // First stop the EtlManagerExecutor process. This stops SQL executions as well.
            try
            {
                if (!StopExecutorProcess(connectionString, executionId))
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
                using SqlConnection sqlConnection = new SqlConnection(connectionString);
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
                encryptionKey = Utility.GetEncryptionKey(encryptionId, connectionString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting encryption key to stop packages and pipelines", executionId);
                return false;
            }

            var stopConfiguration = new StopConfiguration(connectionString, executionId, encryptionKey, username);

            // Start package and pipeline stopping simultaneously.
            var stopPackagesTask = StopPackageExecutionsAsync(stopConfiguration);
            var stopPipelinesTask = StopPipelineExecutionsAsync(stopConfiguration);

            var stopPackagesResult = stopPackagesTask.Result;
            var stopPipelinesResult = stopPipelinesTask.Result;
            
            return stopPackagesResult && stopPipelinesResult;
        }

        private static bool StopExecutorProcess(string connectionString, string executionId)
        {
            int executorProcessId;

            try
            {
                // Get the process id for the execution.
                using SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                SqlCommand fetchProcessId = new SqlCommand(
                    "SELECT TOP 1 ExecutorProcessId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId"
                    , sqlConnection);
                fetchProcessId.Parameters.AddWithValue("@ExecutionId", executionId);
                var result = fetchProcessId.ExecuteScalar();
                if (result == null)
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

        private static async Task<bool> StopPackageExecutionsAsync(StopConfiguration stopConfiguration)
        {
            List<PackageStep> packageSteps = new List<PackageStep>();
            List<Task<bool>> stopTasks = new List<Task<bool>>();

            Log.Information("{ExecutionId} Getting package executions to be stopped", stopConfiguration.ExecutionId);
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(stopConfiguration.ConnectionString);
                sqlConnection.Open();

                SqlCommand fetchPackageStepDetails = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, PackageOperationId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionKey) AS ConnectionString
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PackageOperationId IS NOT NULL"
                    , sqlConnection);
                fetchPackageStepDetails.Parameters.AddWithValue("@ExecutionId", stopConfiguration.ExecutionId);
                fetchPackageStepDetails.Parameters.AddWithValue("@EncryptionKey", stopConfiguration.EncryptionKey);

                using SqlDataReader packageStepReader = await fetchPackageStepDetails.ExecuteReaderAsync();
                while (packageStepReader.Read())
                {
                    string stepId = packageStepReader["StepId"].ToString();
                    int retryAttemptIndex = (int)packageStepReader["RetryAttemptIndex"];
                    long packageOperationId = (long)packageStepReader["PackageOperationId"];
                    string packageConnectionString = packageStepReader["ConnectionString"].ToString();
                    var packageStep = new PackageStep()
                    {
                        StepId = stepId,
                        RetryAttemptIndex = retryAttemptIndex,
                        PackageOperationId = packageOperationId,
                        ConnectionString = packageConnectionString
                    };
                    packageSteps.Add(packageStep);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error reading package executions", stopConfiguration.ExecutionId);
                return false;
            }

            foreach (var step in packageSteps)
            {
                stopTasks.Add(StopPackageStepAsync(stopConfiguration, step));
            }

            bool[] results = await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

        private static async Task<bool> StopPackageStepAsync(StopConfiguration stopConfiguration, PackageStep step)
        {
            Log.Information("{ExecutionId} {StepId} Stopping package operation id {PackageOperationId}", stopConfiguration.ExecutionId, step.StepId, step.PackageOperationId);
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(step.ConnectionString);
                await sqlConnection.OpenAsync();
                SqlCommand stopPackageOperationCmd = new SqlCommand("EXEC SSISDB.catalog.stop_operation @OperationId", sqlConnection) { CommandTimeout = 60 }; // One minute
                stopPackageOperationCmd.Parameters.AddWithValue("@OperationId", step.PackageOperationId);
                await stopPackageOperationCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error stopping package operation id {operationId}", stopConfiguration.ExecutionId, step.StepId, step.PackageOperationId);
                return false;
            }

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(stopConfiguration.ConnectionString);
                await sqlConnection.OpenAsync();
                SqlCommand updateStatus = new SqlCommand(
                  @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(),
                    StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                ExecutionStatus = 'STOPPED',
                    StoppedBy = @Username
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatus.Parameters.AddWithValue("@ExecutionId", stopConfiguration.ExecutionId);
                updateStatus.Parameters.AddWithValue("@StepId", step.StepId);
                updateStatus.Parameters.AddWithValue("@RetryAttemptIndex", step.RetryAttemptIndex);

                if (stopConfiguration.Username != null) updateStatus.Parameters.AddWithValue("@Username", stopConfiguration.Username);
                else updateStatus.Parameters.AddWithValue("@Username", DBNull.Value);
                
                await updateStatus.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error logging SSIS step as stopped", stopConfiguration.ExecutionId, step.StepId);
                return false;
            }
            Log.Information("{ExecutionId} {StepId} Successfully stopped package operation id {PackageOperationId}", stopConfiguration.ExecutionId, step.StepId, step.PackageOperationId);
            return true;
        }

        private static async Task<bool> StopPipelineExecutionsAsync(StopConfiguration stopConfiguration)
        {
            // List containing pairs of DataFactoryIds and pipeline steps.
            List<KeyValuePair<string, PipelineStep>> pipelineRuns = new List<KeyValuePair<string, PipelineStep>>();

            Log.Information("{ExecutionId} Getting pipeline executions to be stopped", stopConfiguration.ExecutionId);
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(stopConfiguration.ConnectionString);
                sqlConnection.Open();

                SqlCommand pipelineSteps = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, PipelineRunId, DataFactoryId
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PipelineRunId IS NOT NULL"
                    , sqlConnection);
                pipelineSteps.Parameters.AddWithValue("@ExecutionId", stopConfiguration.ExecutionId);
                pipelineSteps.Parameters.AddWithValue("@EncryptionKey", stopConfiguration.EncryptionKey);

                using SqlDataReader pipelineStepReader = await pipelineSteps.ExecuteReaderAsync();
                while (pipelineStepReader.Read())
                {
                    string stepId = pipelineStepReader["StepId"].ToString();
                    int retryAttemptIndex = (int)pipelineStepReader["RetryAttemptIndex"];
                    string runId = pipelineStepReader["PipelineRunId"].ToString();
                    string dataFactoryId = pipelineStepReader["DataFactoryId"].ToString();
                    var pipelineStep = new PipelineStep()
                    {
                        StepId = stepId,
                        RetryAttemptIndex = retryAttemptIndex,
                        PipelineRunId = runId,
                        DataFactoryId = dataFactoryId
                    };
                    pipelineRuns.Add(new KeyValuePair<string, PipelineStep>(dataFactoryId, pipelineStep));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error reading pipeline executions", stopConfiguration.ExecutionId);
                return false;
            }

            // Group pipeline steps based on their DataFactoryId.
            var pipelineRunsGrouped = pipelineRuns
                .GroupBy(run => run.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(run => run.Value).ToList()
                );

            List<Task<bool>> stopTasks = new List<Task<bool>>();

            // Iterate the various Data Factories and the runs.
            foreach (string dataFactoryId in pipelineRunsGrouped.Keys)
            {
                DataFactory dataFactory;
                try
                {
                    dataFactory = DataFactory.GetDataFactory(stopConfiguration.ConnectionString, dataFactoryId, stopConfiguration.EncryptionKey);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} Error getting details for Data Factory id {dataFactoryId}", stopConfiguration.ExecutionId, dataFactoryId);
                    return false;
                }

                try
                {
                    dataFactory.CheckAccessTokenValidity(stopConfiguration.ConnectionString);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} Error checking Data Factory {dataFactoryId} access token validity", stopConfiguration.ExecutionId, dataFactoryId);
                    return false;
                }

                var credentials = new TokenCredentials(dataFactory.AccessToken);
                var client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };

                var steps = pipelineRunsGrouped[dataFactoryId];
                foreach (var step in steps)
                {
                    try
                    {
                        if (!dataFactory.CheckAccessTokenValidity(stopConfiguration.ConnectionString))
                        {
                            credentials = new TokenCredentials(dataFactory.AccessToken);
                            client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "{ExecutionId} Error checking Data Factory {dataFactoryId} access token validity", stopConfiguration.ExecutionId, dataFactoryId);
                        return false;
                    }
                    stopTasks.Add(StopPipelineRunAsync(stopConfiguration, step, dataFactory, client));
                }
            }

            var results = await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

        private static async Task<bool> StopPipelineRunAsync(StopConfiguration stopConfiguration, PipelineStep step, DataFactory dataFactory, DataFactoryManagementClient client)
        {
            Log.Information("{ExecutionId} {StepId} Stopping pipeline run id {PipelineRunId}", stopConfiguration.ExecutionId, step.StepId, step.PipelineRunId);
            try
            {
                await client.PipelineRuns.CancelAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName, step.PipelineRunId, isRecursive: true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error stopping pipeline run {runId}", stopConfiguration.ExecutionId, step.StepId, step.PipelineRunId);
                return false;
            }

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(stopConfiguration.ConnectionString);
                sqlConnection.Open();

                SqlCommand updateStatuses = new SqlCommand(
                  @"UPDATE etlmanager.Execution
                        SET EndDateTime = GETDATE(),
                            StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                        ExecutionStatus = 'STOPPED',
                            StoppedBy = @Username
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatuses.Parameters.AddWithValue("@ExecutionId", stopConfiguration.ExecutionId);
                updateStatuses.Parameters.AddWithValue("@StepId", step.StepId);
                updateStatuses.Parameters.AddWithValue("@RetryAttemptIndex", step.RetryAttemptIndex);

                if (stopConfiguration.Username != null) updateStatuses.Parameters.AddWithValue("@Username", stopConfiguration.Username);
                else updateStatuses.Parameters.AddWithValue("@Username", DBNull.Value);

                await updateStatuses.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error logging pipeline step as stopped", stopConfiguration.ExecutionId, step.StepId);
                return false;
            }
            Log.Information("{ExecutionId} {StepId} Successfully stopped pipeline run id {PipelineRunId}", stopConfiguration.ExecutionId, step.StepId, step.PipelineRunId);
            return true;
        }

    }

}
