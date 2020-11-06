using Microsoft.Azure.Management.DataFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        string ExecutionId { get; set; }

        string Username { get; set; }

        string EncryptionKey { get; set; }

        string EtlManagerConnectionString { get; set; }

        public async Task<bool> Run(string executionId, string username, string encryptionKey)
        {
            ExecutionId = executionId;
            Username = username;
            EtlManagerConnectionString = configuration.GetValue<string>("EtlManagerConnectionString");

            // First stop the EtlManagerExecutor process. This stops SQL executions as well.
            try
            {
                if (!StopExecutorProcess())
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error stopping Executor process", ExecutionId);
                return false;
            }

            // Log SQL steps as STOPPED.
            Log.Information("{ExecutionId} Logging SQL steps as stopped", ExecutionId);
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                await sqlConnection.OpenAsync();
                SqlCommand updateStatuses = new SqlCommand(
                  @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(),
                    StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                ExecutionStatus = 'STOPPED',
                    StoppedBy = @Username
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND StepType = 'SQL'"
                    , sqlConnection);
                updateStatuses.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                if (username != null) updateStatuses.Parameters.AddWithValue("@Username", username);
                else updateStatuses.Parameters.AddWithValue("@Username", DBNull.Value);
                await updateStatuses.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error logging SQL steps as stopped", ExecutionId);
            }

            // Get encryption key for package and pipeline stopping.
            if (encryptionKey == null)
            {
                try
                {
                    EncryptionKey = Utility.GetEncryptionKey(EtlManagerConnectionString);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} Error getting encryption key to stop packages and pipelines", ExecutionId);
                    return false;
                }
            }
            else
            {
                EncryptionKey = encryptionKey;
            }

            // Start package and pipeline stopping simultaneously.
            var stopPackagesTask = StopPackageExecutions();
            var stopPipelinesTask = StopPipelineExecutions();

            var stopPackagesResult = stopPackagesTask.Result;
            var stopPipelinesResult = stopPipelinesTask.Result;
            
            return stopPackagesResult && stopPipelinesResult;
        }

        private bool StopExecutorProcess()
        {
            int executorProcessId;

            try
            {
                // Get the process id for the execution.
                using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                sqlConnection.Open();

                SqlCommand fetchProcessId = new SqlCommand(
                    "SELECT TOP 1 ExecutorProcessId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId"
                    , sqlConnection);
                fetchProcessId.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                executorProcessId = (int)fetchProcessId.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error getting Executor process id", ExecutionId);
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
                Log.Warning(ex, "{ExecutionId} Error stopping Executor process. The process id {executorProcessId} is not running", ExecutionId, executorProcessId);
                return false;
            }

            string processName = executorProcess.ProcessName;
            if (!processName.Equals("EtlManagerExecutor"))
            {
                Log.Warning("{ExecutionId} Process id {executorProcessId} does not map to an instance of EtlManagerExecutor", ExecutionId, executorProcessId);
                return false;
            }

            Log.Information("{ExecutionId} Killing Executor process id {executorProcessId}", ExecutionId, executorProcessId);

            executorProcess.Kill();
            executorProcess.WaitForExit();

            return true;
        }

        private async Task<bool> StopPackageExecutions()
        {
            List<PackageStep> packageSteps = new List<PackageStep>();
            List<Task<bool>> stopTasks = new List<Task<bool>>();

            Log.Information("{ExecutionId} Getting package executions to be stopped", ExecutionId);
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                sqlConnection.Open();

                SqlCommand fetchPackageStepDetails = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, PackageOperationId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionKey) AS ConnectionString
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PackageOperationId IS NOT NULL"
                    , sqlConnection);
                fetchPackageStepDetails.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                fetchPackageStepDetails.Parameters.AddWithValue("@EncryptionKey", EncryptionKey);

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
                Log.Error(ex, "{ExecutionId} Error reading package executions", ExecutionId);
                return false;
            }

            foreach (var step in packageSteps)
            {
                stopTasks.Add(StopPackageStep(step));
            }

            bool[] results = await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

        private async Task<bool> StopPackageStep(PackageStep step)
        {
            Log.Information("{ExecutionId} {StepId} Stopping package operation id {PackageOperationId}", ExecutionId, step.StepId, step.PackageOperationId);
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
                Log.Error(ex, "{ExecutionId} {StepId} Error stopping package operation id {operationId}", ExecutionId, step.StepId, step.PackageOperationId);
                return false;
            }

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                await sqlConnection.OpenAsync();
                SqlCommand updateStatus = new SqlCommand(
                  @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(),
                    StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                ExecutionStatus = 'STOPPED',
                    StoppedBy = @Username
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatus.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                updateStatus.Parameters.AddWithValue("@StepId", step.StepId);
                updateStatus.Parameters.AddWithValue("@RetryAttemptIndex", step.RetryAttemptIndex);

                if (Username != null) updateStatus.Parameters.AddWithValue("@Username", Username);
                else updateStatus.Parameters.AddWithValue("@Username", DBNull.Value);
                
                await updateStatus.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error logging SSIS step as stopped", ExecutionId, step.StepId);
                return false;
            }
            Log.Information("{ExecutionId} {StepId} Successfully stopped package operation id {PackageOperationId}", ExecutionId, step.StepId, step.PackageOperationId);
            return true;
        }

        private async Task<bool> StopPipelineExecutions()
        {
            List<KeyValuePair<string, PipelineStep>> pipelineRuns = new List<KeyValuePair<string, PipelineStep>>();

            Log.Information("{ExecutionId} Getting pipeline executions to be stopped", ExecutionId);
            try
            {
                using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                sqlConnection.Open();

                SqlCommand pipelineSteps = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, PipelineRunId, DataFactoryId
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PipelineRunId IS NOT NULL"
                    , sqlConnection);
                pipelineSteps.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                pipelineSteps.Parameters.AddWithValue("@EncryptionKey", EncryptionKey);

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
                Log.Error(ex, "{ExecutionId} Error reading pipeline executions", ExecutionId);
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
                    dataFactory = Utility.GetDataFactory(EtlManagerConnectionString, dataFactoryId, EncryptionKey);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} Error getting details for Data Factory id {dataFactoryId}", ExecutionId, dataFactoryId);
                    return false;
                }

                try
                {
                    dataFactory.CheckAccessTokenValidity(EtlManagerConnectionString);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} Error checking Data Factory {dataFactoryId} access token validity", ExecutionId, dataFactoryId);
                    return false;
                }

                var credentials = new TokenCredentials(dataFactory.AccessToken);
                var client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };

                var steps = pipelineRunsGrouped[dataFactoryId];
                foreach (var step in steps)
                {
                    try
                    {
                        if (!dataFactory.CheckAccessTokenValidity(EtlManagerConnectionString))
                        {
                            credentials = new TokenCredentials(dataFactory.AccessToken);
                            client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "{ExecutionId} Error checking Data Factory {dataFactoryId} access token validity", ExecutionId, dataFactoryId);
                        return false;
                    }
                    stopTasks.Add(StopPipelineRun(step, dataFactory, client));
                }
            }

            var results = await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

        private async Task<bool> StopPipelineRun(PipelineStep step, DataFactory dataFactory, DataFactoryManagementClient client)
        {
            Log.Information("{ExecutionId} {StepId} Stopping pipeline run id {PipelineRunId}", ExecutionId, step.StepId, step.PipelineRunId);
            try
            {
                await client.PipelineRuns.CancelAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName, step.PipelineRunId, isRecursive: true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error stopping pipeline run {runId}", ExecutionId, step.StepId, step.PipelineRunId);
                return false;
            }

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                sqlConnection.Open();

                SqlCommand updateStatuses = new SqlCommand(
                  @"UPDATE etlmanager.Execution
                        SET EndDateTime = GETDATE(),
                            StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                        ExecutionStatus = 'STOPPED',
                            StoppedBy = @Username
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatuses.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                updateStatuses.Parameters.AddWithValue("@StepId", step.StepId);
                updateStatuses.Parameters.AddWithValue("@RetryAttemptIndex", step.RetryAttemptIndex);

                if (Username != null) updateStatuses.Parameters.AddWithValue("@Username", Username);
                else updateStatuses.Parameters.AddWithValue("@Username", DBNull.Value);

                await updateStatuses.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error logging pipeline step as stopped", ExecutionId, step.StepId);
                return false;
            }
            Log.Information("{ExecutionId} {StepId} Successfully stopped pipeline run id {PipelineRunId}", ExecutionId, step.StepId, step.PipelineRunId);
            return true;
        }
    }

    internal class PipelineStep
    {
        public string StepId { get; set; }
        public int RetryAttemptIndex { get; set; }
        public string PipelineRunId { get; set; }
        public string DataFactoryId { get; set; }
    }

    internal class PackageStep
    {
        public string StepId { get; set; }
        public int RetryAttemptIndex { get; set; }
        public long PackageOperationId { get; set; }
        public string ConnectionString { get; set; }
    }

}
