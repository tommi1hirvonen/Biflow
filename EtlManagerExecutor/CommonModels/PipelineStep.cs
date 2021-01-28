using Microsoft.Azure.Management.DataFactory;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class PipelineStep : StepBase, ICancelable
    {
        public string DataFactoryId { get; init; }
        
        public int RetryAttemptIndex { get; init; }
        
        public string PipelineRunId { get; set; }
        protected DataFactoryManagementClient Client { get; set; }
        protected DataFactory DataFactory { get; set; }

        public PipelineStep(ConfigurationBase configuration, string stepId, string dataFactoryId)
            : base(configuration, stepId)
        {
            DataFactoryId = dataFactoryId;
        }

        public async Task<bool> CancelAsync()
        {
            if (DataFactory == null)
            {
                // Get the target Data Factory information from the database.
                try
                {
                    DataFactory = await DataFactory.GetDataFactoryAsync(ConfigurationBase.ConnectionString, DataFactoryId, ConfigurationBase.EncryptionKey);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting Data Factory information for id {DataFactoryId}", DataFactoryId);
                    throw;
                }
            }

            if (Client == null)
            {
                try
                {
                    // Check if the current access token is valid and get a new one if not.
                    await DataFactory.CheckAccessTokenValidityAsync(ConfigurationBase.ConnectionString);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error checking access token validity for pipeline run {runId}", ConfigurationBase.ExecutionId, StepId, PipelineRunId);
                    return false;
                }

                var credentials = new TokenCredentials(DataFactory.AccessToken);
                Client = new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };
            }

            Log.Information("{ExecutionId} {StepId} Stopping pipeline run id {PipelineRunId}", ConfigurationBase.ExecutionId, StepId, PipelineRunId);
            try
            {
                await Client.PipelineRuns.CancelAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, PipelineRunId, isRecursive: true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error stopping pipeline run {runId}", ConfigurationBase.ExecutionId, StepId, PipelineRunId);
                return false;
            }

            try
            {
                using var sqlConnection = new SqlConnection(ConfigurationBase.ConnectionString);
                await sqlConnection.OpenAsync();

                var updateStatuses = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET EndDateTime = GETDATE(),
                        StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                    ExecutionStatus = 'STOPPED',
                        StoppedBy = @Username
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatuses.Parameters.AddWithValue("@ExecutionId", ConfigurationBase.ExecutionId);
                updateStatuses.Parameters.AddWithValue("@StepId", StepId);
                updateStatuses.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptIndex);

                if (ConfigurationBase.Username != null) updateStatuses.Parameters.AddWithValue("@Username", ConfigurationBase.Username);
                else updateStatuses.Parameters.AddWithValue("@Username", DBNull.Value);

                await updateStatuses.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error logging pipeline step as stopped", ConfigurationBase.ExecutionId, StepId);
                return false;
            }
            Log.Information("{ExecutionId} {StepId} Successfully stopped pipeline run id {PipelineRunId}", ConfigurationBase.ExecutionId, StepId, PipelineRunId);
            return true;
        }
    }
}
