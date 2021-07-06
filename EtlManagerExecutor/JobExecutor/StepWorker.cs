using Dapper;
using Serilog;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class StepWorker
    {
        private ExecutionConfiguration Configuration { get; init; }
        private Step Step { get; init; }

        public StepWorker(ExecutionConfiguration executionConfiguration, Step step)
        {
            Configuration = executionConfiguration;
            Step = step;
        }

        public async Task<bool> ExecuteStepAsync(CancellationToken cancellationToken)
        {
            // If the step was canceled already before it was even started, update the status to STOPPED.
            if (cancellationToken.IsCancellationRequested)
            {
                await UpdateExecutionStoppedAsync();
                return false;
            }
            
            StepExecutionBase stepExecution;
            int retryAttempts;
            int retryIntervalMinutes;

            // Get step details.
            using (var sqlConnection = new SqlConnection(Configuration.ConnectionString))
            {
                await sqlConnection.OpenAsync(CancellationToken.None);

                // Check whether this step is already running (in another execution). Only include executions from the past 24 hours.
                try
                {
                    var duplicateExecution = await IsDuplicateExecutionAsync(sqlConnection);
                    // This step execution should be marked as duplicate.
                    if (duplicateExecution)
                    {
                        await UpdateStepAsDuplicateAsync(sqlConnection);
                        Log.Warning("{ExecutionId} {Step} Marked step as DUPLICATE", Configuration.ExecutionId, Step);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error marking step as DUPLICATE", Configuration.ExecutionId, Step);
                    return false;
                }

                // Fetch step details.
                string stepType;
                try
                {
                    (stepType, retryAttempts, retryIntervalMinutes) = await sqlConnection.QueryFirstAsync<(string, int, int)>(
                        @"SELECT TOP 1 StepType, RetryAttempts, RetryIntervalMinutes
                        FROM etlmanager.Execution with (nolock)
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId",
                        new { Configuration.ExecutionId, Step.StepId });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error reading execution details", Configuration.ExecutionId, Step);
                    return false;
                }

                try
                {
                    IStepExecutionBuilder stepExecutionBuilder = stepType switch
                    {
                        "SQL" => new SqlStepExecutionBuilder(),
                        "SSIS" => new PackageStepExecutionBuilder(),
                        "JOB" => new JobStepExecutionBuilder(),
                        "PIPELINE" => new PipelineStepExecutionBuilder(),
                        "EXE" => new ExeStepExecutionBuilder(),
                        "DATASET" => new DatasetStepExecutionBuilder(),
                        _ => throw new InvalidOperationException($"{stepType} is not a recognized step type")
                    };
                    stepExecution = await stepExecutionBuilder.CreateAsync(Configuration, Step, sqlConnection);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error creating step execution object", Configuration.ExecutionId, Step);
                    return false;
                }
            }

            // Loop until there are not retry attempts left.
            while (stepExecution.RetryAttemptCounter <= retryAttempts)
            {
                await CheckIfStepExecutionIsRetryAttemptAsync(stepExecution);

                // Execute the step based on its step type.
                ExecutionResult executionResult;
                try
                {
                    executionResult = await stepExecution.ExecuteAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await UpdateExecutionCancelledAsync(stepExecution);
                    return false;
                }
                catch (Exception ex)
                {
                    executionResult = new ExecutionResult.Failure("Error during step execution: " + ex.Message);
                }

                if (executionResult is ExecutionResult.Failure failureResult)
                {
                    Log.Warning("{ExecutionId} {Step} Error executing step: " + failureResult.ErrorMessage, Configuration.ExecutionId, Step);
                    await UpdateExecutionFailedAsync(stepExecution, retryAttempts, failureResult);

                    // There are attempts left => increase counter and wait for the retry interval.
                    if (stepExecution.RetryAttemptCounter < retryAttempts)
                    {
                        stepExecution.RetryAttemptCounter++;
                        try
                        {
                            await WaitForExecutionRetryAsync(stepExecution, retryIntervalMinutes, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            return false;
                        }
                    }
                    // Otherwise break the loop and end this execution.
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Log.Information("{ExecutionId} {Step} Step executed successfully", Configuration.ExecutionId, Step);
                    // The step execution was successful. Update the execution accordingly.
                    await UpdateExecutionSucceededAsync(stepExecution, executionResult);
                    return true; // Break the loop to end this execution.
                }
            }

            return false; // Execution should not arrive here in normal conditions. Return false.
        }

        private async Task WaitForExecutionRetryAsync(StepExecutionBase stepExecution, int retryIntervalMinutes, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(retryIntervalMinutes * 60 * 1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // If the step was canceled during waiting for a retry, copy a new execution row with STOPPED status.
                Log.Warning("{ExecutionId} {Step} Step was canceled", Configuration.ExecutionId, Step);
                try
                {
                    using var connection = new SqlConnection(Configuration.ConnectionString);
                    await connection.ExecuteAsync(
                        @"EXEC [etlmanager].[ExecutionStepCopy] @ExecutionId = @ExecutionId_, @StepId = @StepId_, @RetryAttemptIndex = @RetryAttemptIndex_, @Status = 'STOPPED'",
                        new { ExecutionId_ = Configuration.ExecutionId, StepId_ = Step.StepId, RetryAttemptIndex_ = stepExecution.RetryAttemptCounter });
                    // Update end time for the newly added row.
                    await connection.ExecuteAsync(
                        @"UPDATE etlmanager.Execution
                        SET EndDateTime = GETDATE()
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex",
                        new { Configuration.ExecutionId, Step.StepId, RetryAttemptIndex = stepExecution.RetryAttemptCounter });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error copying step execution details for retry attempt", Configuration.ExecutionId, Step);
                }
                throw;
            }
        }

        private async Task UpdateExecutionCancelledAsync(StepExecutionBase stepExecution)
        {
            // Execution was canceled. Update the status to STOPPED and do not attempt any retries (return).
            using var connection = new SqlConnection(Configuration.ConnectionString);
            await connection.ExecuteAsync(
                @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(), ExecutionStatus = 'STOPPED', StoppedBy = @Username
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex",
                new
                {
                    Configuration.ExecutionId,
                    Step.StepId,
                    RetryAttemptIndex = stepExecution.RetryAttemptCounter,
                    Username = (object)Configuration.Username ?? DBNull.Value
                });
        }

        private async Task UpdateExecutionFailedAsync(StepExecutionBase stepExecution, int retryAttempts, ExecutionResult.Failure failureResult)
        {
            // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
            var status = stepExecution.RetryAttemptCounter >= retryAttempts ? "FAILED" : "AWAIT RETRY";
            try
            {
                using var connection = new SqlConnection(Configuration.ConnectionString);
                await connection.ExecuteAsync(
                    @"UPDATE etlmanager.Execution
                    SET EndDateTime = GETDATE(), ExecutionStatus = @ExecutionStatus, ErrorMessage = @ErrorMessage, InfoMessage = @InfoMessage
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex",
                    new
                    {
                        Configuration.ExecutionId,
                        Step.StepId,
                        RetryAttemptIndex = stepExecution.RetryAttemptCounter,
                        ExecutionStatus = status,
                        failureResult.ErrorMessage,
                        failureResult.InfoMessage
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating step status to {status}", Configuration.ExecutionId, Step, status);
            }
        }

        private async Task UpdateExecutionSucceededAsync(StepExecutionBase stepExecution, ExecutionResult executionResult)
        {
            try
            {
                using var connection = new SqlConnection(Configuration.ConnectionString);
                await connection.ExecuteAsync(
                    @"UPDATE etlmanager.Execution
                    SET EndDateTime = GETDATE(), ExecutionStatus = 'SUCCEEDED', InfoMessage = @InfoMessage
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex",
                    new
                    {
                        Configuration.ExecutionId,
                        Step.StepId,
                        RetryAttemptIndex = stepExecution.RetryAttemptCounter,
                        executionResult.InfoMessage
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating step status to SUCCEEDED", Configuration.ExecutionId, Step);
            }
        }

        private async Task UpdateExecutionStoppedAsync()
        {
            using var connection = new SqlConnection(Configuration.ConnectionString);
            await connection.ExecuteAsync(
                @"UPDATE etlmanager.Execution
                SET EndDateTime = GETDATE(), ExecutionStatus = 'STOPPED', StoppedBy = @Username
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId",
                new { Configuration.ExecutionId, Step.StepId, Configuration.Username });
        }

        private async Task CheckIfStepExecutionIsRetryAttemptAsync(StepExecutionBase stepExecution)
        {
            using var connection = new SqlConnection(Configuration.ConnectionString);
            // In case of first execution, update the existing execution row.
            if (stepExecution.RetryAttemptCounter == 0)
            {
                try
                {
                    await connection.ExecuteAsync(
                        @"UPDATE etlmanager.Execution
                        SET StartDateTime = GETDATE(), ExecutionStatus = 'RUNNING'
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex",
                        new { Configuration.ExecutionId, Step.StepId, RetryAttemptIndex = stepExecution.RetryAttemptCounter });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error updating step status to RUNNING", Configuration.ExecutionId, Step);
                }
            }
            // In case of later retry execution, insert new execution row based on the first one (RetryAttemptIndex = 0).
            else
            {
                try
                {
                    await connection.ExecuteAsync(
                        @"EXEC [etlmanager].[ExecutionStepCopy] @ExecutionId = @ExecutionId_, @StepId = @StepId_, @RetryAttemptIndex = @RetryAttemptIndex_, @Status = 'RUNNING'",
                        new { ExecutionId_ = Configuration.ExecutionId, StepId_ = Step.StepId, RetryAttemptIndex_ = stepExecution.RetryAttemptCounter });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error copying step execution details for retry attempt", Configuration.ExecutionId, Step);
                }
            }
        }

        private async Task<bool> IsDuplicateExecutionAsync(SqlConnection sqlConnection)
        {
            var rows = (await sqlConnection.QueryAsync(
                @"SELECT 1
                FROM etlmanager.Execution
                WHERE StepId = @StepId AND ExecutionStatus = 'RUNNING' AND StartDateTime >= DATEADD(DAY, -1, GETDATE())",
                new { Step.StepId })).ToList();
            return rows.Any();
        }

        private async Task UpdateStepAsDuplicateAsync(SqlConnection sqlConnection)
        {
            await sqlConnection.ExecuteAsync(
                @"UPDATE etlmanager.Execution
                SET ExecutionStatus = 'DUPLICATE',
                StartDateTime = GETDATE(), EndDateTime = GETDATE()
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId",
                new { Configuration.ExecutionId, Step.StepId });
        }

    }

}
