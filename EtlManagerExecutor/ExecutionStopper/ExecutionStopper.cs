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

            var stopConfiguration = new StopConfiguration(connectionString, executionId, encryptionKey, username);

            // Start package and pipeline stopping simultaneously.
            var stopPackagesTask = StopPackageExecutionsAsync(stopConfiguration);
            var stopPipelinesTask = StopPipelineExecutionsAsync(stopConfiguration);

            var stopPackagesResult = stopPackagesTask.Result;
            var stopPipelinesResult = stopPipelinesTask.Result;
            
            return stopPackagesResult && stopPipelinesResult;
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
            var packageSteps = new List<PackageStep>();
            var stopTasks = new List<Task<bool>>();

            Log.Information("{ExecutionId} Getting package executions to be stopped", stopConfiguration.ExecutionId);
            try
            {
                using var sqlConnection = new SqlConnection(stopConfiguration.ConnectionString);
                await sqlConnection.OpenAsync();

                var fetchPackageStepDetails = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, PackageOperationId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionKey) AS ConnectionString
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PackageOperationId IS NOT NULL"
                    , sqlConnection);
                fetchPackageStepDetails.Parameters.AddWithValue("@ExecutionId", stopConfiguration.ExecutionId);
                fetchPackageStepDetails.Parameters.AddWithValue("@EncryptionKey", stopConfiguration.EncryptionKey);

                using var packageStepReader = await fetchPackageStepDetails.ExecuteReaderAsync();
                while (await packageStepReader.ReadAsync())
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
                stopTasks.Add(step.StopExecutionAsync(stopConfiguration));
            }

            bool[] results = await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

        private static async Task<bool> StopPipelineExecutionsAsync(StopConfiguration stopConfiguration)
        {
            // List containing pairs of DataFactoryIds and pipeline steps.
            var pipelineRuns = new List<KeyValuePair<string, PipelineStep>>();

            Log.Information("{ExecutionId} Getting pipeline executions to be stopped", stopConfiguration.ExecutionId);
            try
            {
                using var sqlConnection = new SqlConnection(stopConfiguration.ConnectionString);
                await sqlConnection.OpenAsync();

                var pipelineSteps = new SqlCommand(
                    @"SELECT StepId, RetryAttemptIndex, PipelineRunId, DataFactoryId
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PipelineRunId IS NOT NULL"
                    , sqlConnection);
                pipelineSteps.Parameters.AddWithValue("@ExecutionId", stopConfiguration.ExecutionId);
                pipelineSteps.Parameters.AddWithValue("@EncryptionKey", stopConfiguration.EncryptionKey);

                using var pipelineStepReader = await pipelineSteps.ExecuteReaderAsync();
                while (await pipelineStepReader.ReadAsync())
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
            Dictionary<string, List<PipelineStep>> pipelineRunsGrouped = pipelineRuns
                .GroupBy(run => run.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(run => run.Value).ToList()
                );

            var stopTasks = new List<Task<bool>>();

            // Iterate the various Data Factories and the runs.
            foreach (string dataFactoryId in pipelineRunsGrouped.Keys)
            {
                DataFactory dataFactory;
                try
                {
                    dataFactory = await DataFactory.GetDataFactoryAsync(stopConfiguration.ConnectionString, dataFactoryId, stopConfiguration.EncryptionKey);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} Error getting details for Data Factory id {dataFactoryId}", stopConfiguration.ExecutionId, dataFactoryId);
                    return false;
                }

                try
                {
                    await dataFactory.CheckAccessTokenValidityAsync(stopConfiguration.ConnectionString);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} Error checking Data Factory {dataFactoryId} access token validity", stopConfiguration.ExecutionId, dataFactoryId);
                    return false;
                }

                var credentials = new TokenCredentials(dataFactory.AccessToken);
                var client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };

                List<PipelineStep> steps = pipelineRunsGrouped[dataFactoryId];
                foreach (var step in steps)
                {
                    try
                    {
                        if (!await dataFactory.CheckAccessTokenValidityAsync(stopConfiguration.ConnectionString))
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
                    stopTasks.Add(step.StopPipelineRunAsync(stopConfiguration, dataFactory, client));
                }
            }

            bool[] results = await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            return results.All(b => b == true);
        }

    }

}
