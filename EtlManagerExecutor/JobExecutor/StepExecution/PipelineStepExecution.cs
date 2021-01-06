using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Rest;
using Serilog;
using System;
using System.Data.SqlClient;
using System.Threading;

namespace EtlManagerExecutor
{
    class PipelineStepExecution : IStepExecution
    {
        private readonly ExecutionConfiguration executionConfig;
        private readonly PipelineStepConfiguration pipelineStep;
        private readonly int retryAttempt;

        private const int PollingIntervalMs = 5000;
        private const int MaxRefreshRetries = 5;

        public PipelineStepExecution(ExecutionConfiguration executionConfiguration, PipelineStepConfiguration pipelineStepConfiguration, int retryAttempt)
        {
            executionConfig = executionConfiguration;
            pipelineStep = pipelineStepConfiguration;
            this.retryAttempt = retryAttempt;
        }

        public ExecutionResult Run()
        {
            DataFactory dataFactory;

            // Get the target Data Factory information from the database.
            try
            {
                dataFactory = DataFactory.GetDataFactory(executionConfig.ConnectionString, pipelineStep.DataFactoryId, executionConfig.EncryptionPassword);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Data Factory information for id {DataFactoryId}", pipelineStep.DataFactoryId);
                throw;
            }

            // Check if the current access token is valid and get a new one if not.
            dataFactory.CheckAccessTokenValidity(executionConfig.ConnectionString);

            var credentials = new TokenCredentials(dataFactory.AccessToken);
            var client = new DataFactoryManagementClient(credentials) { SubscriptionId = dataFactory.SubscriptionId };

            CreateRunResponse createRunResponse;
            try
            {
                createRunResponse = client.Pipelines.CreateRun(dataFactory.ResourceGroupName, dataFactory.ResourceName, pipelineStep.PipelineName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error creating pipeline run for Data Factory id {DataFactoryId} and pipeline {PipelineName}",
                    executionConfig.ExecutionId, pipelineStep.StepId, pipelineStep.DataFactoryId, pipelineStep.PipelineName);
                throw;
            }

            string runId = createRunResponse.RunId;

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(executionConfig.ConnectionString);
                sqlConnection.Open();
                SqlCommand sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET PipelineRunId = @PipelineRunId
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex", sqlConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", pipelineStep.StepId);
                sqlCommand.Parameters.AddWithValue("@RetryAttemptIndex", retryAttempt);
                sqlCommand.Parameters.AddWithValue("@PipelineRunId", runId);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "{ExecutionId} {StepId} Error updating pipeline run id", executionConfig.ExecutionId, pipelineStep.StepId);
            }

            PipelineRun pipelineRun;
            while (true)
            {
                if (!dataFactory.CheckAccessTokenValidity(executionConfig.ConnectionString))
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
                    Log.Warning(ex, "{ExecutionId} {StepId} Error getting pipeline run status for run id {runId}",
                        executionConfig.ExecutionId, pipelineStep.StepId, runId);
                    refreshRetries++;
                    Thread.Sleep(PollingIntervalMs);
                }
            }
            throw new TimeoutException("The maximum number of pipeline run status refresh attempts was reached.");
        }

    }
}
