using Serilog;
using System;
using System.Data.SqlClient;
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
                    using var stepDetailsCmd = new SqlCommand(
                        @"SELECT TOP 1 StepType, RetryAttempts, RetryIntervalMinutes
                        FROM etlmanager.Execution with (nolock)
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                        , sqlConnection);
                    stepDetailsCmd.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                    stepDetailsCmd.Parameters.AddWithValue("@StepId", Step.StepId);
                    using var reader = await stepDetailsCmd.ExecuteReaderAsync(CancellationToken.None);
                    if (await reader.ReadAsync(CancellationToken.None))
                    {
                        stepType = reader["StepType"].ToString()!;
                        retryAttempts = (int)reader["RetryAttempts"];
                        retryIntervalMinutes = (int)reader["RetryIntervalMinutes"];
                    }
                    else
                    {
                        Log.Error("{ExecutionId} {Step} Could not find execution details", Configuration.ExecutionId, Step);
                        return false;
                    }
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
                    await connection.OpenAsync(CancellationToken.None);
                    using var addNewExecution = new SqlCommand(
                        @"EXEC [etlmanager].[ExecutionStepCopy] @ExecutionId = @ExecutionId_, @StepId = @StepId_, @RetryAttemptIndex = @RetryAttemptIndex_, @Status = 'STOPPED'"
                        , connection);
                    addNewExecution.Parameters.AddWithValue("@ExecutionId_", Configuration.ExecutionId);
                    addNewExecution.Parameters.AddWithValue("@StepId_", Step.StepId);
                    addNewExecution.Parameters.AddWithValue("@RetryAttemptIndex_", stepExecution.RetryAttemptCounter);
                    await addNewExecution.ExecuteNonQueryAsync(CancellationToken.None);

                    using var updateEndTime = new SqlCommand(
                        @"UPDATE etlmanager.Execution
                        SET EndDateTime = GETDATE()
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                        , connection);
                    updateEndTime.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                    updateEndTime.Parameters.AddWithValue("@StepId", Step.StepId);
                    updateEndTime.Parameters.AddWithValue("@RetryAttemptIndex", stepExecution.RetryAttemptCounter);
                    await updateEndTime.ExecuteNonQueryAsync(CancellationToken.None);
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
            await connection.OpenAsync(CancellationToken.None);
            using var canceledUpdate = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = 'STOPPED', StoppedBy = @Username
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                    , connection);
            canceledUpdate.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
            canceledUpdate.Parameters.AddWithValue("@StepId", Step.StepId);
            canceledUpdate.Parameters.AddWithValue("@RetryAttemptIndex", stepExecution.RetryAttemptCounter);
            canceledUpdate.Parameters.AddWithValue("@Username", (object)Configuration.Username ?? DBNull.Value);
            await canceledUpdate.ExecuteNonQueryAsync(CancellationToken.None);
        }

        private async Task UpdateExecutionFailedAsync(StepExecutionBase stepExecution, int retryAttempts, ExecutionResult.Failure failureResult)
        {
            // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
            var status = stepExecution.RetryAttemptCounter >= retryAttempts ? "FAILED" : "AWAIT RETRY";
            try
            {
                using var connection = new SqlConnection(Configuration.ConnectionString);
                await connection.OpenAsync(CancellationToken.None);
                using var errorUpdate = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = @ExecutionStatus, ErrorMessage = @ErrorMessage, InfoMessage = @InfoMessage
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                    , connection);
                errorUpdate.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                errorUpdate.Parameters.AddWithValue("@StepId", Step.StepId);
                errorUpdate.Parameters.AddWithValue("@RetryAttemptIndex", stepExecution.RetryAttemptCounter);
                errorUpdate.Parameters.AddWithValue("@ExecutionStatus", status);
                errorUpdate.Parameters.AddWithValue("@ErrorMessage", failureResult.ErrorMessage);
                errorUpdate.Parameters.AddWithValue("@InfoMessage", failureResult.InfoMessage?.Length > 0 ? (object)failureResult.InfoMessage : DBNull.Value);
                await errorUpdate.ExecuteNonQueryAsync(CancellationToken.None);
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
                await connection.OpenAsync(CancellationToken.None);
                using var successUpdate = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = 'SUCCEEDED', InfoMessage = @InfoMessage
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                    , connection);
                successUpdate.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                successUpdate.Parameters.AddWithValue("@StepId", Step.StepId);
                successUpdate.Parameters.AddWithValue("@RetryAttemptIndex", stepExecution.RetryAttemptCounter);
                successUpdate.Parameters.AddWithValue("@InfoMessage", executionResult.InfoMessage?.Length > 0 ? (object)executionResult.InfoMessage : DBNull.Value);
                await successUpdate.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating step status to SUCCEEDED", Configuration.ExecutionId, Step);
            }
        }

        private async Task UpdateExecutionStoppedAsync()
        {
            using var connection = new SqlConnection(Configuration.ConnectionString);
            await connection.OpenAsync(CancellationToken.None);
            using var canceledUpdate = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = 'STOPPED', StoppedBy = @Username
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                    , connection);
            canceledUpdate.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
            canceledUpdate.Parameters.AddWithValue("@StepId", Step.StepId);
            canceledUpdate.Parameters.AddWithValue("@Username", (object)Configuration.Username ?? DBNull.Value);
            await canceledUpdate.ExecuteNonQueryAsync(CancellationToken.None);
        }

        private async Task CheckIfStepExecutionIsRetryAttemptAsync(StepExecutionBase stepExecution)
        {
            using var connection = new SqlConnection(Configuration.ConnectionString);
            await connection.OpenAsync(CancellationToken.None); // Open the connection to ETL Manager database for execution start logging.
                                                                // In case of first execution, update the existing execution row.
            if (stepExecution.RetryAttemptCounter == 0)
            {
                try
                {
                    using var startUpdate = new SqlCommand(
                      @"UPDATE etlmanager.Execution
                                SET StartDateTime = GETDATE(), ExecutionStatus = 'RUNNING'
                                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                        , connection);
                    startUpdate.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                    startUpdate.Parameters.AddWithValue("@StepId", Step.StepId);
                    startUpdate.Parameters.AddWithValue("@RetryAttemptIndex", stepExecution.RetryAttemptCounter);
                    await startUpdate.ExecuteNonQueryAsync(CancellationToken.None);
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
                    using var addNewExecution = new SqlCommand(
                      @"EXEC [etlmanager].[ExecutionStepCopy] @ExecutionId = @ExecutionId_, @StepId = @StepId_, @RetryAttemptIndex = @RetryAttemptIndex_, @Status = 'RUNNING'"
                        , connection);
                    addNewExecution.Parameters.AddWithValue("@ExecutionId_", Configuration.ExecutionId);
                    addNewExecution.Parameters.AddWithValue("@StepId_", Step.StepId);
                    addNewExecution.Parameters.AddWithValue("@RetryAttemptIndex_", stepExecution.RetryAttemptCounter);
                    await addNewExecution.ExecuteNonQueryAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error copying step execution details for retry attempt", Configuration.ExecutionId, Step);
                }
            }
        }

        private async Task<bool> IsDuplicateExecutionAsync(SqlConnection sqlConnection)
        {
            using var duplicateExecutionCmd = new SqlCommand(
                        @"SELECT 1
                        FROM etlmanager.Execution
                        WHERE StepId = @StepId AND ExecutionStatus = 'RUNNING' AND StartDateTime >= DATEADD(DAY, -1, GETDATE())"
                        , sqlConnection);
            duplicateExecutionCmd.Parameters.AddWithValue("@StepId", Step.StepId);
            using var duplicateReader = await duplicateExecutionCmd.ExecuteReaderAsync();
            // If the duplicate execution query returns any rows, there is another execution running for the same step.
            if (duplicateReader.HasRows)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task UpdateStepAsDuplicateAsync(SqlConnection sqlConnection)
        {
            var duplicateCommand = new SqlCommand(
                                        @"UPDATE etlmanager.Execution
                            SET ExecutionStatus = 'DUPLICATE',
                            StartDateTime = GETDATE(), EndDateTime = GETDATE()
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                                        , sqlConnection)
            {
                CommandTimeout = 120 // two minutes
            };
            duplicateCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
            duplicateCommand.Parameters.AddWithValue("@StepId", Step.StepId);
            await duplicateCommand.ExecuteNonQueryAsync(CancellationToken.None);
        }

    }

}
