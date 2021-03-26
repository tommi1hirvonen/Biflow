using EtlManagerUtils;
using Microsoft.Azure.Management.DataFactory.Models;
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
        private DataFactoryHelper DataFactoryHelper { get; set; }
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
                DataFactoryHelper = await DataFactoryHelper.GetDataFactoryHelperAsync(Configuration.ConnectionString, DataFactoryId, Configuration.EncryptionKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Data Factory information for id {DataFactoryId}", DataFactoryId);
                return new ExecutionResult.Failure($"Error getting Data Factory object information:\n{ex.Message}");
            }

            string runId;
            DateTime startTime;
            try
            {
                runId = await DataFactoryHelper.StartPipelineRunAsync(PipelineName, parameters, cancellationToken);
                startTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error creating pipeline run for Data Factory id {DataFactoryId} and pipeline {PipelineName}",
                    Configuration.ExecutionId, Step, DataFactoryHelper.DataFactoryId, PipelineName);
                return new ExecutionResult.Failure($"Error starting pipeline run:\n{ex.Message}");
            }

            try
            {
                using var sqlConnection = new SqlConnection(Configuration.ConnectionString);
                await sqlConnection.OpenAsync(CancellationToken.None);
                using var sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET PipelineRunId = @PipelineRunId
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex", sqlConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", Step.StepId);
                sqlCommand.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptCounter);
                sqlCommand.Parameters.AddWithValue("@PipelineRunId", runId);
                await sqlCommand.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "{ExecutionId} {Step} Error updating pipeline run id", Configuration.ExecutionId, Step);
            }

            PipelineRun pipelineRun;
            while (true)
            {
                try
                {
                    pipelineRun = await TryGetPipelineRunAsync(runId, cancellationToken);
                    if (pipelineRun.Status == "InProgress" || pipelineRun.Status == "Queued")
                    {
                        // Check for timeout.
                        if (TimeoutMinutes > 0 && (DateTime.Now - startTime).TotalMinutes > TimeoutMinutes)
                        {
                            await CancelAsync(runId);
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
                    await CancelAsync(runId);
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

        private async Task<Dictionary<string, object>> GetStepParameters()
        {
            var parameters = new Dictionary<string, object>();

            using var sqlConnection = new SqlConnection(Configuration.ConnectionString);
            using var paramsCommand = new SqlCommand(
                @"SELECT ParameterName, ParameterValue
                    FROM etlmanager.ExecutionParameter
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND ParameterLevel = 'Pipeline'"
                , sqlConnection);
            paramsCommand.Parameters.AddWithValue("@StepId", Step.StepId);
            paramsCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);

            await sqlConnection.OpenAsync();
            using var reader = await paramsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader["ParameterName"].ToString();
                object value = reader["ParameterValue"];
                parameters[name] = value;
            }

            return parameters;
        }

        private async Task<PipelineRun> TryGetPipelineRunAsync(string runId, CancellationToken cancellationToken)
        {
            int refreshRetries = 0;
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    return await DataFactoryHelper.GetPipelineRunAsync(runId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "{ExecutionId} {Step} Error getting pipeline run status for run id {runId}", Configuration.ExecutionId, Step, runId);
                    refreshRetries++;
                    await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
                }
            }
            throw new TimeoutException("The maximum number of pipeline run status refresh attempts was reached.");
        }

        private async Task CancelAsync(string runId)
        {
            Log.Information("{ExecutionId} {Step} Stopping pipeline run id {PipelineRunId}", Configuration.ExecutionId, Step, runId);
            try
            {
                await DataFactoryHelper.CancelPipelineRunAsync(runId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}", Configuration.ExecutionId, Step, runId);
            }
        }
    }
}
