using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Rest;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class PipelineStepExecution : StepExecutionBase
    {
        private string DataFactoryId { get; init; }
        private string PipelineRunId { get; set; }
        private DataFactoryManagementClient Client { get; set; }
        private DataFactory DataFactory { get; set; }
        private string PipelineName { get; init; }
        private int TimeoutMinutes { get; init; }

        private const int MaxRefreshRetries = 3;

        public PipelineStepExecution(ExecutionConfiguration configuration, Step step, string dataFactoryId, string pipelineName, int timeoutMinutes)
            : base(configuration, step)
        {
            DataFactoryId = dataFactoryId;
            PipelineName = pipelineName;
            TimeoutMinutes = timeoutMinutes;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get possible parameters.
            IDictionary<string, object> parameters;
            try
            {
                parameters = await GetStepParameters();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error retrieving pipeline parameters", Configuration.ExecutionId, Step);
                return new ExecutionResult.Failure("Error reading pipeline parameters: " + ex.Message);
            }


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
                createRunResponse = await Client.Pipelines.CreateRunAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, PipelineName,
                    parameters: parameters, cancellationToken: CancellationToken.None);
                startTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error creating pipeline run for Data Factory id {DataFactoryId} and pipeline {PipelineName}",
                    Configuration.ExecutionId, Step, DataFactory.DataFactoryId, PipelineName);
                throw;
            }

            PipelineRunId = createRunResponse.RunId;

            try
            {
                using var sqlConnection = new SqlConnection(Configuration.ConnectionString);
                await sqlConnection.OpenAsync(CancellationToken.None);
                var sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET PipelineRunId = @PipelineRunId
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex", sqlConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", Step.StepId);
                sqlCommand.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptCounter);
                sqlCommand.Parameters.AddWithValue("@PipelineRunId", PipelineRunId);
                await sqlCommand.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "{ExecutionId} {Step} Error updating pipeline run id", Configuration.ExecutionId, Step);
            }

            PipelineRun pipelineRun;
            while (true)
            {
                if (!await DataFactory.CheckAccessTokenValidityAsync(Configuration.ConnectionString))
                {
                    credentials = new TokenCredentials(DataFactory.AccessToken);
                    Client = new DataFactoryManagementClient(credentials) { SubscriptionId = DataFactory.SubscriptionId };
                }

                try
                {
                    pipelineRun = await TryGetPipelineRunAsync(cancellationToken);
                    if (pipelineRun.Status == "InProgress" || pipelineRun.Status == "Queued")
                    {
                        // Check for timeout.
                        if (TimeoutMinutes > 0 && (DateTime.Now - startTime).TotalMinutes > TimeoutMinutes)
                        {
                            await CancelAsync();
                            Log.Warning("{ExecutionId} {Step} Step execution timed out", Configuration.ExecutionId, Step);
                            return new ExecutionResult.Failure("Step execution timed out");
                        }

                        await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    await CancelAsync();
                    throw;
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

        private async Task<PipelineRun> TryGetPipelineRunAsync(CancellationToken cancellationToken)
        {
            int refreshRetries = 0;
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    return await Client.PipelineRuns.GetAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, PipelineRunId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "{ExecutionId} {Step} Error getting pipeline run status for run id {runId}", Configuration.ExecutionId, Step, PipelineRunId);
                    refreshRetries++;
                    await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
                }
            }
            throw new TimeoutException("The maximum number of pipeline run status refresh attempts was reached.");
        }

        public async Task CancelAsync()
        {
            Log.Information("{ExecutionId} {Step} Stopping pipeline run id {PipelineRunId}", Configuration.ExecutionId, Step, PipelineRunId);
            try
            {
                await Client.PipelineRuns.CancelAsync(DataFactory.ResourceGroupName, DataFactory.ResourceName, PipelineRunId, isRecursive: true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}", Configuration.ExecutionId, Step, PipelineRunId);
            }
        }
    }
}
