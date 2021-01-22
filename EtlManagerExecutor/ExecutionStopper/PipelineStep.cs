using Microsoft.Azure.Management.DataFactory;
using Serilog;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    partial class ExecutionStopper
    {
        internal class PipelineStep
        {
            public string StepId { get; set; }
            public int RetryAttemptIndex { get; set; }
            public string PipelineRunId { get; set; }
            public string DataFactoryId { get; set; }

            public async Task<bool> StopPipelineRunAsync(StopConfiguration stopConfiguration, DataFactory dataFactory, DataFactoryManagementClient client)
            {
                Log.Information("{ExecutionId} {StepId} Stopping pipeline run id {PipelineRunId}", stopConfiguration.ExecutionId, StepId, PipelineRunId);
                try
                {
                    await client.PipelineRuns.CancelAsync(dataFactory.ResourceGroupName, dataFactory.ResourceName, PipelineRunId, isRecursive: true);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error stopping pipeline run {runId}", stopConfiguration.ExecutionId, StepId, PipelineRunId);
                    return false;
                }

                try
                {
                    using var sqlConnection = new SqlConnection(stopConfiguration.ConnectionString);
                    await sqlConnection.OpenAsync();

                    var updateStatuses = new SqlCommand(
                      @"UPDATE etlmanager.Execution
                        SET EndDateTime = GETDATE(),
                            StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                        ExecutionStatus = 'STOPPED',
                            StoppedBy = @Username
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                        , sqlConnection);
                    updateStatuses.Parameters.AddWithValue("@ExecutionId", stopConfiguration.ExecutionId);
                    updateStatuses.Parameters.AddWithValue("@StepId", StepId);
                    updateStatuses.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptIndex);

                    if (stopConfiguration.Username != null) updateStatuses.Parameters.AddWithValue("@Username", stopConfiguration.Username);
                    else updateStatuses.Parameters.AddWithValue("@Username", DBNull.Value);

                    await updateStatuses.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error logging pipeline step as stopped", stopConfiguration.ExecutionId, StepId);
                    return false;
                }
                Log.Information("{ExecutionId} {StepId} Successfully stopped pipeline run id {PipelineRunId}", stopConfiguration.ExecutionId, StepId, PipelineRunId);
                return true;
            }

        }

    }

}
