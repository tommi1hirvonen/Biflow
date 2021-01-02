using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Rest;
using Serilog;
using System;
using System.Data.SqlClient;
using System.Threading;

namespace EtlManagerExecutor
{
    class PipelineExecution
    {
        private string ExecutionId { get; set; }
        private string StepId { get; set; }
        private int AttemptCounter { get; set; }
        private string EtlManagerConnectionString { get; set; }
        private string DataFactoryId { get; set; }
        private string EncryptionKey { get; set; }
        private string PipelineName { get; set; }

        private const int PollingIntervalMs = 5000;
        private const int MaxRefreshRetries = 5;

        public PipelineExecution(string executionId, string stepId, int attemptCounter, string etlManagerConnectionString, string dataFactoryId, string encryptionKey, string pipelineName)
        {
            ExecutionId = executionId;
            StepId = stepId;
            AttemptCounter = attemptCounter;
            EtlManagerConnectionString = etlManagerConnectionString;
            DataFactoryId = dataFactoryId;
            EncryptionKey = encryptionKey;
            PipelineName = pipelineName;
        }

        public ExecutionResult Run()
        {
            DataFactory dataFactory;

            // Get the target Data Factory information from the database.
            try
            {
                dataFactory = Utility.GetDataFactory(EtlManagerConnectionString, DataFactoryId, EncryptionKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Data Factory information for id {DataFactoryId}", DataFactoryId);
                throw;
            }

            // Check if the current access token is valid and get a new one if not.
            dataFactory.CheckAccessTokenValidity(EtlManagerConnectionString);

            var credentials = new TokenCredentials(dataFactory.AccessToken);
            var client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };

            CreateRunResponse createRunResponse;
            try
            {
                createRunResponse = client.Pipelines.CreateRun(dataFactory.ResourceGroupName, dataFactory.ResourceName, PipelineName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error creating pipeline run for Data Factory id {DataFactoryId} and pipeline {PipelineName}", ExecutionId, StepId, DataFactoryId, PipelineName);
                throw;
            }

            string runId = createRunResponse.RunId;

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString);
                sqlConnection.Open();
                SqlCommand sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET PipelineRunId = @PipelineRunId
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex", sqlConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", StepId);
                sqlCommand.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                sqlCommand.Parameters.AddWithValue("@PipelineRunId", runId);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "{ExecutionId} {StepId} Error updating pipeline run id", ExecutionId, StepId);
            }

            PipelineRun pipelineRun;
            while (true)
            {
                if (!dataFactory.CheckAccessTokenValidity(EtlManagerConnectionString))
                {
                    credentials = new TokenCredentials(dataFactory.AccessToken);
                    client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };
                }

                pipelineRun = TryGetPipelineRun(dataFactory, client, runId);

                if (pipelineRun.Status == "InProgress" || pipelineRun.Status == "Queued")
                {
                    Thread.Sleep(5000);
                }
                else
                {
                    break;
                }
            }

            if (pipelineRun.Status == "Succeeded")
            {
                return new ExecutionResult.Success();
            }
            else
            {
                return new ExecutionResult.Failure(pipelineRun.Message);
            }
        }

        private PipelineRun TryGetPipelineRun(DataFactory dataFactory, DataFactoryManagementClient client, string runId)
        {
            int refreshRetries = 0;
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    return client.PipelineRuns.Get(dataFactory.ResourceGroupName, dataFactory.ResourceName, runId);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "{ExecutionId} {StepId} Error getting pipeline run status for run id {runId}", ExecutionId, StepId, runId);
                    refreshRetries++;
                    Thread.Sleep(PollingIntervalMs);
                }
            }
            throw new TimeoutException("The maximum number of pipeline run status refresh attempts was reached.");
        }

    }
}
