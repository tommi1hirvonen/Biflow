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
                using var connection = new SqlConnection(Configuration.ConnectionString);
                await connection.OpenAsync(CancellationToken.None);
                var canceledUpdate = new SqlCommand(
                        @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = 'STOPPED', StoppedBy = @Username
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                        , connection);
                canceledUpdate.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                canceledUpdate.Parameters.AddWithValue("@StepId", Step.StepId);
                canceledUpdate.Parameters.AddWithValue("@Username", (object)Configuration.Username ?? DBNull.Value);
                await canceledUpdate.ExecuteNonQueryAsync(CancellationToken.None);
                return false;
            }

            StepExecutionBase stepExecution;
            int retryAttempts;
            int retryIntervalMinutes;
            int timeoutMinutes;

            // Get step details.
            using (var sqlConnection = new SqlConnection(Configuration.ConnectionString))
            {
                await sqlConnection.OpenAsync(CancellationToken.None);

                // Check whether this step is already running (in another execution). Only include executions from the past 24 hours.
                var duplicateExecutionCmd = new SqlCommand(
                    @"SELECT 1
                    FROM etlmanager.Execution
                    WHERE StepId = @StepId AND ExecutionStatus = 'RUNNING' AND StartDateTime >= DATEADD(DAY, -1, GETDATE())"
                    , sqlConnection);
                duplicateExecutionCmd.Parameters.AddWithValue("@StepId", Step.StepId);

                try
                {
                    using var duplicateReader = await duplicateExecutionCmd.ExecuteReaderAsync(CancellationToken.None);
                    // If the duplicate execution query returns any rows, there is another execution running for the same step.
                    // This step execution should be marked as duplicate.
                    if (duplicateReader.HasRows)
                    {
                        await duplicateReader.CloseAsync();
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
                var stepDetailsCmd = new SqlCommand(
                    @"SELECT TOP 1 StepType, SqlStatement, ConnectionId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionPassword) AS ConnectionString,
                        PackageFolderName, PackageProjectName, PackageName,
                        ExecuteIn32BitMode, JobToExecuteId, JobExecuteSynchronized, RetryAttempts, RetryIntervalMinutes, TimeoutMinutes, DataFactoryId, PipelineName
                    FROM etlmanager.Execution
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                    , sqlConnection);
                stepDetailsCmd.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                stepDetailsCmd.Parameters.AddWithValue("@StepId", Step.StepId);
                stepDetailsCmd.Parameters.AddWithValue("@EncryptionPassword", Configuration.EncryptionKey);
                
                try
                {
                    using var reader = await stepDetailsCmd.ExecuteReaderAsync(CancellationToken.None);
                    if (await reader.ReadAsync(CancellationToken.None))
                    {
                        var stepType = reader["StepType"].ToString();
                        retryAttempts = (int)reader["RetryAttempts"];
                        retryIntervalMinutes = (int)reader["RetryIntervalMinutes"];
                        timeoutMinutes = (int)reader["TimeoutMinutes"];
                        if (stepType == "SQL")
                        {
                            var sqlStatement = reader["SqlStatement"].ToString();
                            var connectionString = reader["ConnectionString"].ToString();
                            stepExecution = new SqlStepExecution(Configuration, Step, sqlStatement, connectionString, timeoutMinutes);
                        }
                        else if (stepType == "SSIS")
                        {
                            var connectionString = reader["ConnectionString"].ToString();
                            var packageFolderName = reader["PackageFolderName"].ToString();
                            var packageProjectName = reader["PackageProjectName"].ToString();
                            var packageName = reader["PackageName"].ToString();
                            var executeIn32BitMode = reader["ExecuteIn32BitMode"].ToString() == "1";
                            stepExecution = new PackageStepExecution(Configuration, Step, connectionString,
                                packageFolderName, packageProjectName, packageName, executeIn32BitMode, timeoutMinutes);
                        }
                        else if (stepType == "JOB")
                        {
                            var jobToExecuteId = reader["JobToExecuteId"].ToString();
                            var jobExecuteSynchronized = (bool)reader["JobExecuteSynchronized"];
                            stepExecution = new JobStepExecution(Configuration, Step, jobToExecuteId, jobExecuteSynchronized);
                        }
                        else if (stepType == "PIPELINE")
                        {
                            var dataFactoryId = reader["DataFactoryId"].ToString();
                            var pipelineName = reader["PipelineName"].ToString();
                            stepExecution = new PipelineStepExecution(Configuration, Step, dataFactoryId, pipelineName, timeoutMinutes);
                        }
                        else
                        {
                            Log.Error("{ExecutionId} {Step} Incorrect step type {stepType}", Configuration.ExecutionId, Step, stepType);
                            return false;
                        }

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
            }

            // Loop until there are not retry attempts left.
            while (stepExecution.RetryAttemptCounter <= retryAttempts)
            {
                using (var connection = new SqlConnection(Configuration.ConnectionString))
                {
                    await connection.OpenAsync(CancellationToken.None); // Open the connection to ETL Manager database for execution start logging.
                    // In case of first execution, update the existing execution row.
                    if (stepExecution.RetryAttemptCounter == 0)
                    {
                        try
                        {
                            var startUpdate = new SqlCommand(
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
                            var addNewExecution = new SqlCommand(
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

                // Execute the step based on its step type.
                ExecutionResult executionResult;
                try
                {
                    executionResult = await stepExecution.ExecuteAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Execution was canceled. Update the status to STOPPED and do not attempt any retries (return).
                    using var connection = new SqlConnection(Configuration.ConnectionString);
                    await connection.OpenAsync(CancellationToken.None);
                    var canceledUpdate = new SqlCommand(
                            @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = 'STOPPED', StoppedBy = @Username
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                            , connection);
                    canceledUpdate.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                    canceledUpdate.Parameters.AddWithValue("@StepId", Step.StepId);
                    canceledUpdate.Parameters.AddWithValue("@RetryAttemptIndex", stepExecution.RetryAttemptCounter);
                    canceledUpdate.Parameters.AddWithValue("@Username", (object)Configuration.Username ?? DBNull.Value);
                    await canceledUpdate.ExecuteNonQueryAsync(CancellationToken.None);
                    return false;
                }
                catch (Exception ex)
                {
                    executionResult = new ExecutionResult.Failure("Error during step execution: " + ex.Message);
                }


                if (executionResult is ExecutionResult.Failure failureResult)
                {
                    Log.Warning("{ExecutionId} {Step} Error executing step: " + failureResult.ErrorMessage, Configuration.ExecutionId, Step);

                    // The step failed. Update the execution accordingly.

                    // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
                    var status = stepExecution.RetryAttemptCounter >= retryAttempts ? "FAILED" : "AWAIT RETRY";

                    try
                    {
                        using var connection = new SqlConnection(Configuration.ConnectionString);
                        await connection.OpenAsync(CancellationToken.None);
                        var errorUpdate = new SqlCommand(
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

                    // The step failed. There are attempts left => increase counter and wait for the retry interval.
                    if (stepExecution.RetryAttemptCounter < retryAttempts)
                    {
                        stepExecution.RetryAttemptCounter++;
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
                                var addNewExecution = new SqlCommand(
                                    @"EXEC [etlmanager].[ExecutionStepCopy] @ExecutionId = @ExecutionId_, @StepId = @StepId_, @RetryAttemptIndex = @RetryAttemptIndex_, @Status = 'STOPPED'"
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

                    // The package was executed successfully. Update the execution accordingly.
                    try
                    {
                        using var connection = new SqlConnection(Configuration.ConnectionString);
                        await connection.OpenAsync(CancellationToken.None);
                        var successUpdate = new SqlCommand(
                            @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = 'COMPLETED', InfoMessage = @InfoMessage
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
                        Log.Error(ex, "{ExecutionId} {Step} Error updating step status to COMPLETED", Configuration.ExecutionId, Step);
                    }

                    return true; // Break the loop to end this execution.
                }
                
            }

            return false; // Execution should not arrive here in normal conditions. Return false.
        }

    }

}
