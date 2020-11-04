using Microsoft.Azure.Management.DataFactory;
using Microsoft.Extensions.Configuration;
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

        public async Task Run(string executionId, string username)
        {
            string etlManagerConnectionString = configuration.GetValue<string>("EtlManagerConnectionString");
            using SqlConnection sqlConnection = new SqlConnection(etlManagerConnectionString);
            await sqlConnection.OpenAsync();

            // First stop the EtlManagerExecutor process. This stops SQL executions as well.
            try
            {
                StopExecutorProcess(sqlConnection, executionId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error stopping Executor process");
            }

            List<Task> stopTasks = new List<Task>
            {
                // Then start the process to stop package executions
                StopPackageExecutions(etlManagerConnectionString, sqlConnection, executionId),

                // Then start the process to stop pipeline executions
                StopPipelineExecutions(etlManagerConnectionString, sqlConnection, executionId)
            };

            await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.

            SqlCommand updateStatuses = new SqlCommand(
              @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(),
                    StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                ExecutionStatus = 'STOPPED',
                    StoppedBy = @Username
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL"
                , sqlConnection);
            updateStatuses.Parameters.AddWithValue("@ExecutionId", executionId);
            if (username != null) updateStatuses.Parameters.AddWithValue("@Username", username);
            else updateStatuses.Parameters.AddWithValue("@Username", DBNull.Value);
            
            await updateStatuses.ExecuteNonQueryAsync();
        }

        private static void StopExecutorProcess(SqlConnection sqlConnection, string executionId)
        {
            // Get the process id for the execution.
            SqlCommand fetchProcessId = new SqlCommand(
                "SELECT TOP 1 ExecutorProcessId FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId"
                , sqlConnection);
            fetchProcessId.Parameters.AddWithValue("@ExecutionId", executionId);
            int executorProcessId = (int)fetchProcessId.ExecuteScalar();

            // Get the process and check that its name matches so that we don't accidentally kill the wrong process.
            Process executorProcess = Process.GetProcessById(executorProcessId);
            string processName = executorProcess.ProcessName;
            if (!processName.Equals("EtlManagerExecutor"))
            {
                throw new ArgumentException("Process id does not map to an instance of EtlManagerExecutor");
            }
            
            
                executorProcess.Kill();
                executorProcess.WaitForExit();
            
        }

        private async Task StopPackageExecutions(string connectionString, SqlConnection sqlConnection, string executionId)
        {
            List<Task> stopTasks = new List<Task>();

            string encryptionKey = Utility.GetEncryptionKey(connectionString);

            SqlCommand fetchPackageOperationIds = new SqlCommand(
                @"SELECT PackageOperationId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionKey) AS ConnectionString
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PackageOperationId IS NOT NULL"
                , sqlConnection);
            fetchPackageOperationIds.Parameters.AddWithValue("@ExecutionId", executionId);
            fetchPackageOperationIds.Parameters.AddWithValue("@EncryptionKey", encryptionKey);

            using (SqlDataReader packageOperationReader = await fetchPackageOperationIds.ExecuteReaderAsync())
            {
                while (packageOperationReader.Read())
                {
                    long packageOperationId = (long)packageOperationReader[0];
                    string packageConnectionString = null;
                    if (!packageOperationReader.IsDBNull(1)) packageConnectionString = (string)packageOperationReader[1];

                    stopTasks.Add(StopPackage(packageConnectionString, packageOperationId));
                }
            }

            await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.
        }

        private async static Task StopPackage(string connectionString, long operationId)
        {
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            SqlCommand stopPackageOperationCmd = new SqlCommand("EXEC SSISDB.catalog.stop_operation @OperationId", sqlConnection) { CommandTimeout = 60 }; // One minute
            stopPackageOperationCmd.Parameters.AddWithValue("@OperationId", operationId);
            await stopPackageOperationCmd.ExecuteNonQueryAsync();
        }

        private async static Task StopPipelineExecutions(string connectionString, SqlConnection sqlConnection, string executionId)
        {
            List<Task> stopTasks = new List<Task>();

            string encryptionKey = Utility.GetEncryptionKey(connectionString);

            SqlCommand fetchRunIds = new SqlCommand(
                @"SELECT PipelineRunId, DataFactoryId
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND EndDateTime IS NULL AND PipelineRunId IS NOT NULL"
                , sqlConnection);
            fetchRunIds.Parameters.AddWithValue("@ExecutionId", executionId);
            fetchRunIds.Parameters.AddWithValue("@EncryptionKey", encryptionKey);

            List<KeyValuePair<string, string>> pipelineRuns = new List<KeyValuePair<string, string>>();

            using (SqlDataReader runIdReader = await fetchRunIds.ExecuteReaderAsync())
            {
                while (runIdReader.Read())
                {
                    string runId = runIdReader[0].ToString();
                    string dataFactoryId = runIdReader[1].ToString();
                    pipelineRuns.Add(new KeyValuePair<string, string>(dataFactoryId, runId));
                }
            }

            var pipelineRunsGrouped = pipelineRuns
                .GroupBy(run => run.Key)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(run => run.Value).ToList()
                );

            // Iterate the various Data Factories and the runs.
            foreach (string dataFactoryId in pipelineRunsGrouped.Keys)
            {

                var runIds = pipelineRunsGrouped[dataFactoryId];
                foreach (string runId in runIds)
                {

                }
            }

            await Task.WhenAll(stopTasks); // Wait for all stop commands to finish.
        }

        private async static Task StopPipelineRun(DataFactoryManagementClient client, string resourceGroupName, string resourceName, string runId)
        {
            await client.PipelineRuns.CancelAsync(resourceGroupName, resourceName, runId, isRecursive: true);
        }
    }
}
