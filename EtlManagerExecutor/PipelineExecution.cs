using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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

        private DataFactory DataFactory { get; set; }

        private string PipelineName { get; set; }

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
            // Get the target Data Factory information from the database.
            try
            {
                DataFactory = Utility.GetDataFactory(EtlManagerConnectionString, DataFactoryId, EncryptionKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Data Factory information for id {DataFactoryId}", DataFactoryId);
                throw ex;
            }

            // Check if the current access token is valid and get a new one if not.
            DataFactory.CheckAccessTokenValidity(EtlManagerConnectionString);

            var credentials = new TokenCredentials(DataFactory.AccessToken);
            var client = new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };

            CreateRunResponse createRunResponse;
            try
            {
                createRunResponse = client.Pipelines.CreateRun(DataFactory.ResourceGroupName, DataFactory.ResourceName, PipelineName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating pipeline run for Data Factory id {DataFactoryId} and pipeline {PipelineName}", DataFactoryId, PipelineName);
                throw ex;
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
                Log.Warning(ex, "Error updating pipeline run id");
            }

            PipelineRun pipelineRun;
            while (true)
            {
                if (!DataFactory.CheckAccessTokenValidity(EtlManagerConnectionString))
                {
                    credentials = new TokenCredentials(DataFactory.AccessToken);
                    client = new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };
                }

                try
                {
                    pipelineRun = client.PipelineRuns.Get(DataFactory.ResourceGroupName, DataFactory.ResourceName, runId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error getting pipeline run status for Data Factory id {DataFactoryId}, pipeline {PipelineName}, run id {runId}", DataFactoryId, PipelineName, runId);
                    throw ex;
                }

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

    }
}
