using Dapper;
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
    class PipelineStepExecutionBuilder : IStepExecutionBuilder
    {
        public async Task<StepExecutionBase> CreateAsync(ExecutionConfiguration config, Step step, SqlConnection sqlConnection)
        {
            (var dataFactoryId, var pipelineName, var timeoutMinutes) = await sqlConnection.QueryFirstAsync<(Guid, string, int)>(
                @"SELECT TOP 1 DataFactoryId, PipelineName, TimeoutMinutes
                FROM etlmanager.Execution with (nolock)
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId",
                new { config.ExecutionId, step.StepId });
            return new PipelineStepExecution(config, step, dataFactoryId, pipelineName, timeoutMinutes);
        }
    }

    class PipelineStepExecution : StepExecutionBase
    {
        private Guid DataFactoryId { get; init; }
        private DataFactoryHelper? DataFactoryHelper { get; set; }
        private string PipelineName { get; init; }
        private int TimeoutMinutes { get; init; }

        private const int MaxRefreshRetries = 3;

        public PipelineStepExecution(ExecutionConfiguration configuration, Step step, Guid dataFactoryId, string pipelineName, int timeoutMinutes)
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

            if (Configuration.EncryptionKey is null)
                throw new ArgumentNullException(nameof(Configuration.EncryptionKey), "Encryption key cannot be null for pipeline step executions");

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
                await sqlConnection.ExecuteAsync(
                    @"UPDATE etlmanager.Execution
                    SET PipelineRunId = @PipelineRunId
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex",
                    new { Configuration.ExecutionId, Step.StepId, RetryAttemptIndex = RetryAttemptCounter, PipelineRunId = runId });
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
            using var sqlConnection = new SqlConnection(Configuration.ConnectionString);
            var parameters = await sqlConnection.QueryAsync<(string, object)>(
                @"SELECT ParameterName, ParameterValue
                FROM etlmanager.ExecutionParameter
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND ParameterLevel = 'Pipeline'",
                new { Configuration.ExecutionId, Step.StepId });
            var dictionary = parameters.ToDictionary(key => key.Item1, value => value.Item2);
            return dictionary;
        }

        private async Task<PipelineRun> TryGetPipelineRunAsync(string runId, CancellationToken cancellationToken)
        {
            int refreshRetries = 0;
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    return await DataFactoryHelper!.GetPipelineRunAsync(runId, CancellationToken.None);
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
                await DataFactoryHelper!.CancelPipelineRunAsync(runId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error stopping pipeline run {runId}", Configuration.ExecutionId, Step, runId);
            }
        }
    }
}
