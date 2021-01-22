using Serilog;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    partial class ExecutionStopper
    {
        internal class PackageStep
        {
            public string StepId { get; set; }
            public int RetryAttemptIndex { get; set; }
            public long PackageOperationId { get; set; }
            public string ConnectionString { get; set; }

            public async Task<bool> StopExecutionAsync(StopConfiguration stopConfiguration)
            {
                Log.Information("{ExecutionId} {StepId} Stopping package operation id {PackageOperationId}", stopConfiguration.ExecutionId, StepId, PackageOperationId);
                try
                {
                    using var sqlConnection = new SqlConnection(ConnectionString);
                    await sqlConnection.OpenAsync();
                    var stopPackageOperationCmd = new SqlCommand("EXEC SSISDB.catalog.stop_operation @OperationId", sqlConnection) { CommandTimeout = 60 }; // One minute
                    stopPackageOperationCmd.Parameters.AddWithValue("@OperationId", PackageOperationId);
                    await stopPackageOperationCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error stopping package operation id {operationId}", stopConfiguration.ExecutionId, StepId, PackageOperationId);
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
                    updateStatus.Parameters.AddWithValue("@StepId", StepId);
                    updateStatus.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptIndex);

                    if (stopConfiguration.Username != null) updateStatus.Parameters.AddWithValue("@Username", stopConfiguration.Username);
                    else updateStatus.Parameters.AddWithValue("@Username", DBNull.Value);

                    await updateStatus.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error logging SSIS step as stopped", stopConfiguration.ExecutionId, StepId);
                    return false;
                }
                Log.Information("{ExecutionId} {StepId} Successfully stopped package operation id {PackageOperationId}", stopConfiguration.ExecutionId, StepId, PackageOperationId);
                return true;
            }
        }

    }

}
