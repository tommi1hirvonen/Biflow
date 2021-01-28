using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class PackageStep : StepBase, ICancelable
    {
        public string ConnectionString { get; init; }

        public int RetryAttemptIndex { get; set; }
        public long PackageOperationId { get; set; }

        public PackageStep(ConfigurationBase configuration, string stepId, string connectionString)
            : base(configuration, stepId)
        {
            ConnectionString = connectionString;
        }

        public async Task<bool> CancelAsync()
        {
            Log.Information("{ExecutionId} {StepId} Stopping package operation id {PackageOperationId}", ConfigurationBase.ExecutionId, StepId, PackageOperationId);
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
                Log.Error(ex, "{ExecutionId} {StepId} Error stopping package operation id {operationId}", ConfigurationBase.ExecutionId, StepId, PackageOperationId);
                return false;
            }

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(ConfigurationBase.ConnectionString);
                await sqlConnection.OpenAsync();
                SqlCommand updateStatus = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET EndDateTime = GETDATE(),
                        StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                    ExecutionStatus = 'STOPPED',
                        StoppedBy = @Username
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatus.Parameters.AddWithValue("@ExecutionId", ConfigurationBase.ExecutionId);
                updateStatus.Parameters.AddWithValue("@StepId", StepId);
                updateStatus.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptIndex);

                if (ConfigurationBase.Username != null) updateStatus.Parameters.AddWithValue("@Username", ConfigurationBase.Username);
                else updateStatus.Parameters.AddWithValue("@Username", DBNull.Value);

                await updateStatus.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error logging SSIS step as stopped", ConfigurationBase.ExecutionId, StepId);
                return false;
            }
            Log.Information("{ExecutionId} {StepId} Successfully stopped package operation id {PackageOperationId}", ConfigurationBase.ExecutionId, StepId, PackageOperationId);
            return true;
        }
    }
}
