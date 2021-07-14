using Dapper;
using EtlManagerDataAccess.Models;
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
    class PipelineStepExecutor : StepExecutorBase
    {
        private PipelineStepExecution Step { get; init; }
        private DataFactoryHelper? DataFactoryHelper { get; set; }

        private const int MaxRefreshRetries = 3;

        public PipelineStepExecutor(ExecutionConfiguration configuration, PipelineStepExecution step) : base(configuration)
        {
            Step = step;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get possible parameters.
            IDictionary<string, object> parameters;
            try
            {
                parameters = Step.StepExecutionParameters
                    .ToDictionary(key => key.ParameterName, value => value.ParameterValue);
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
                DataFactoryHelper = await DataFactoryHelper.GetDataFactoryHelperAsync(Configuration.ConnectionString, Step.DataFactoryId, Configuration.EncryptionKey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Data Factory information for id {DataFactoryId}", Step.DataFactoryId);
                return new ExecutionResult.Failure($"Error getting Data Factory object information:\n{ex.Message}");
            }

            string runId;
            DateTime startTime;
            try
            {
                runId = await DataFactoryHelper.StartPipelineRunAsync(Step.PipelineName, parameters, cancellationToken);
                startTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error creating pipeline run for Data Factory id {DataFactoryId} and pipeline {PipelineName}",
                    Configuration.ExecutionId, Step, DataFactoryHelper.DataFactoryId, Step.PipelineName);
                return new ExecutionResult.Failure($"Error starting pipeline run:\n{ex.Message}");
            }

            try
            {
                using var context = Configuration.DbContextFactory.CreateDbContext();
                var attempt = Step.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == RetryAttemptCounter);
                if (attempt is not null && attempt is PipelineStepExecutionAttempt pipeline)
                {
                    pipeline.PipelineRunId = runId;
                    context.Attach(pipeline);
                    context.Entry(pipeline).Property(e => e.PipelineRunId).IsModified = true;
                    await context.SaveChangesAsync(CancellationToken.None);
                }
                else
                {
                    throw new InvalidOperationException("Could not find step execution attempt to update pipeline run id");
                }
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
                        if (Step.TimeoutMinutes > 0 && (DateTime.Now - startTime).TotalMinutes > Step.TimeoutMinutes)
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
