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

        public async Task<bool> Run(string executionId, string username)
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
            string EncryptionKey;
            try
            {
                EncryptionKey = Utility.GetEncryptionKey(encryptionId, connectionString);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} Error getting encryption key to stop packages and pipelines", executionId);
                return false;
            }
            

            // Start package and pipeline stopping simultaneously.
            var stopPackagesTask = StopPackageExecutions(connectionString, executionId, EncryptionKey, username);
            var stopPipelinesTask = StopPipelineExecutions(connectionString, executionId, EncryptionKey, username);

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

        private static async Task<bool> StopPackageExecutions(string connectionString, string executionId, string encryptionKey, string username)
        {
            List<PackageStep> packageSteps = new List<PackageStep>();
            List<Task<bool>> stopTasks = new List<Task<bool>>();

            Log.Information("{executionId} Getting package executions to be stopped", executionId);
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                SqlCommand fetchPackageStepDetails = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, PackageOperationId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionKey) AS ConnectionString
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PackageOperationId IS NOT NULL"
                    , sqlConnection);
                fetchPackageStepDetails.Parameters.AddWithValue("@ExecutionId", executionId);
                fetchPackageStepDetails.Parameters.AddWithValue("@EncryptionKey", encryptionKey);

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
                Log.Error(ex, "{executionId} Error reading package executions", executionId);
                return false;
            }

            foreach (var step in packageSteps)
            {
                stopTasks.Add(StopPackageStep(connectionString, executionId, username, step));
            }

            bool[] results = await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

        private static async Task<bool> StopPackageStep(string connectionString, string executionId, string username, PackageStep step)
        {
            Log.Information("{executionId} {StepId} Stopping package operation id {PackageOperationId}", executionId, step.StepId, step.PackageOperationId);
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
                Log.Error(ex, "{executionId} {StepId} Error stopping package operation id {operationId}", executionId, step.StepId, step.PackageOperationId);
                return false;
            }

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(connectionString);
                await sqlConnection.OpenAsync();
                SqlCommand updateStatus = new SqlCommand(
                  @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(),
                    StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                ExecutionStatus = 'STOPPED',
                    StoppedBy = @Username
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatus.Parameters.AddWithValue("@ExecutionId", executionId);
                updateStatus.Parameters.AddWithValue("@StepId", step.StepId);
                updateStatus.Parameters.AddWithValue("@RetryAttemptIndex", step.RetryAttemptIndex);

                if (username != null) updateStatus.Parameters.AddWithValue("@Username", username);
                else updateStatus.Parameters.AddWithValue("@Username", DBNull.Value);
                
                await updateStatus.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} {StepId} Error logging SSIS step as stopped", executionId, step.StepId);
                return false;
            }
            Log.Information("{executionId} {StepId} Successfully stopped package operation id {PackageOperationId}", executionId, step.StepId, step.PackageOperationId);
            return true;
        }

        private static async Task<bool> StopPipelineExecutions(string connectionString, string executionId, string encryptionKey, string username)
        {
            List<KeyValuePair<string, PipelineStep>> pipelineRuns = new List<KeyValuePair<string, PipelineStep>>();

            Log.Information("{executionId} Getting pipeline executions to be stopped", executionId);
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                SqlCommand pipelineSteps = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, PipelineRunId, DataFactoryId
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PipelineRunId IS NOT NULL"
                    , sqlConnection);
                pipelineSteps.Parameters.AddWithValue("@ExecutionId", executionId);
                pipelineSteps.Parameters.AddWithValue("@EncryptionKey", encryptionKey);

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
                Log.Error(ex, "{executionId} Error reading pipeline executions", executionId);
                return false;
            }

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
                    dataFactory = DataFactory.GetDataFactory(connectionString, dataFactoryId, encryptionKey);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{executionId} Error getting details for Data Factory id {dataFactoryId}", executionId, dataFactoryId);
                    return false;
                }

                try
                {
                    dataFactory.CheckAccessTokenValidity(connectionString);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{executionId} Error checking Data Factory {dataFactoryId} access token validity", executionId, dataFactoryId);
                    return false;
                }

                var credentials = new TokenCredentials(dataFactory.AccessToken);
                var client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };

                var steps = pipelineRunsGrouped[dataFactoryId];
                foreach (var step in steps)
                {
                    try
                    {
                        if (!dataFactory.CheckAccessTokenValidity(connectionString))
                        {
                            credentials = new TokenCredentials(dataFactory.AccessToken);
                            client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "{executionId} Error checking Data Factory {dataFactoryId} access token validity", executionId, dataFactoryId);
                        return false;
                    }
                    stopTasks.Add(StopPipelineRun(connectionString, executionId, username, step, dataFactory, client));
                }
            }

            var results = await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

        private static async Task<bool> StopPipelineRun(string connectionString, string executionId, string username, PipelineStep step, DataFactory dataFactory, DataFactoryManagementClient client)
        {
            Log.Information("{executionId} {StepId} Stopping pipeline run id {PipelineRunId}", executionId, step.StepId, step.PipelineRunId);
            try
            {
                await client.PipelineRuns.CancelAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName, step.PipelineRunId, isRecursive: true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} {StepId} Error stopping pipeline run {runId}", executionId, step.StepId, step.PipelineRunId);
                return false;
            }

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                SqlCommand updateStatuses = new SqlCommand(
                  @"UPDATE etlmanager.Execution
                        SET EndDateTime = GETDATE(),
                            StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                        ExecutionStatus = 'STOPPED',
                            StoppedBy = @Username
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatuses.Parameters.AddWithValue("@ExecutionId", executionId);
                updateStatuses.Parameters.AddWithValue("@StepId", step.StepId);
                updateStatuses.Parameters.AddWithValue("@RetryAttemptIndex", step.RetryAttemptIndex);

                if (username != null) updateStatuses.Parameters.AddWithValue("@Username", username);
                else updateStatuses.Parameters.AddWithValue("@Username", DBNull.Value);

                await updateStatuses.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{executionId} {StepId} Error logging pipeline step as stopped", executionId, step.StepId);
                return false;
            }
            Log.Information("{executionId} {StepId} Successfully stopped pipeline run id {PipelineRunId}", executionId, step.StepId, step.PipelineRunId);
            return true;
        }

    }

}
