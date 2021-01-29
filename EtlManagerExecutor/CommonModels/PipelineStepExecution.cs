using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
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
    class PipelineStepExecution : PipelineStep, IExecutable
    {
        private ExecutionConfiguration Configuration { get; init; }
        private string PipelineName { get; init; }

        private int TimeoutMinutes { get; init; }

        private const int PollingIntervalMs = 5000;
        private const int MaxRefreshRetries = 5;

        public PipelineStepExecution(ExecutionConfiguration configuration, string stepId, string dataFactoryId, string pipelineName, int timeoutMinutes)
            : base(configuration, stepId, dataFactoryId)
        {
            Configuration = configuration;
            PipelineName = pipelineName;
            TimeoutMinutes = timeoutMinutes;
        }

        public async Task<ExecutionResult> ExecuteAsync()
        {
            // Get the target Data Factory information from the database.
            try
            {
                DataFactory = await DataFactory.GetDataFactoryAsync(Configuration.ConnectionString, DataFactoryId, Configuration.EncryptionKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Data Factory information for id {DataFactoryId}", DataFactoryId);
                throw;
            }

            // Check if the current access token is valid and get a new one if not.
            await DataFactory.CheckAccessTokenValidityAsync(Configuration.ConnectionString);

            var credentials = new TokenCredentials(DataFactory.AccessToken);
            Client = new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };

            CreateRunResponse createRunResponse;
            DateTime startTime;
            try
            {
                createRunResponse = await Client.Pipelines.CreateRunAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, PipelineName);
                startTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error creating pipeline run for Data Factory id {DataFactoryId} and pipeline {PipelineName}",
                    Configuration.ExecutionId, StepId, DataFactory.DataFactoryId, PipelineName);
                throw;
            }

            PipelineRunId = createRunResponse.RunId;

            try
            {
                using var sqlConnection = new SqlConnection(Configuration.ConnectionString);
                await sqlConnection.OpenAsync();
                var sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET PipelineRunId = @PipelineRunId
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex", sqlConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", StepId);
                sqlCommand.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptCounter);
                sqlCommand.Parameters.AddWithValue("@PipelineRunId", PipelineRunId);
                await sqlCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "{ExecutionId} {StepId} Error updating pipeline run id", Configuration.ExecutionId, StepId);
            }

            PipelineRun pipelineRun;
            while (true)
            {
                if (!await DataFactory.CheckAccessTokenValidityAsync(Configuration.ConnectionString))
                {
                    credentials = new TokenCredentials(DataFactory.AccessToken);
                    Client = new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };
                }

                pipelineRun = await TryGetPipelineRunAsync();

                if (pipelineRun.Status == "InProgress" || pipelineRun.Status == "Queued")
                {
                    // Check for timeout.
                    if (TimeoutMinutes > 0 && (DateTime.Now - startTime).TotalMinutes > TimeoutMinutes)
                    {
                        await CancelAsync();
                        Log.Warning("{ExecutionId} {StepId} Step execution timed out", Configuration.ExecutionId, StepId);
                        return new ExecutionResult.Failure("Step execution timed out");
                    }

                    await Task.Delay(PollingIntervalMs);
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

        private async Task<PipelineRun> TryGetPipelineRunAsync()
        {
            int refreshRetries = 0;
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    return Client.PipelineRuns.Get(DataFactory.ResourceGroupName, DataFactory.ResourceName, PipelineRunId);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "{ExecutionId} {StepId} Error getting pipeline run status for run id {runId}",
                        Configuration.ExecutionId, StepId, PipelineRunId);
                    refreshRetries++;
                    await Task.Delay(PollingIntervalMs);
                }
            }
            throw new TimeoutException("The maximum number of pipeline run status refresh attempts was reached.");
        }
    }
}
