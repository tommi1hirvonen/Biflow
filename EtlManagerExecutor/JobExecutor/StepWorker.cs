using Serilog;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class StepWorker
    {
        private readonly ExecutionConfiguration executionConfiguration;
        private readonly string stepId;

        private int AttemptCounter { get; set; } = 0;

        public StepWorker(ExecutionConfiguration executionConfiguration, string stepId)
        {
            this.executionConfiguration = executionConfiguration;
            this.stepId = stepId;
        }

        public async Task ExecuteStepAsync()
        {
            IExecutable stepExecution;
            int retryAttempts;
            int retryIntervalMinutes;
            int timeoutMinutes;

            // Get step details.
            using (var sqlConnection = new SqlConnection(executionConfiguration.ConnectionString))
            {
                var sqlCommand = new SqlCommand(
                    @"SELECT TOP 1 StepType, SqlStatement, ConnectionId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionPassword) AS ConnectionString,
                        PackageFolderName, PackageProjectName, PackageName,
                        ExecuteIn32BitMode, JobToExecuteId, JobExecuteSynchronized, RetryAttempts, RetryIntervalMinutes, TimeoutMinutes, DataFactoryId, PipelineName
                    FROM etlmanager.Execution
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                    , sqlConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", executionConfiguration.ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", stepId);
                sqlCommand.Parameters.AddWithValue("@EncryptionPassword", executionConfiguration.EncryptionKey);
                try
                {
                    await sqlConnection.OpenAsync();
                    using var reader = await sqlCommand.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        var stepType = reader["StepType"].ToString();
                        retryAttempts = (int)reader["RetryAttempts"];
                        retryIntervalMinutes = (int)reader["RetryIntervalMinutes"];
                        timeoutMinutes = (int)reader["TimeoutMinutes"];
                        if (stepType == "SQL")
                        {
                            var sqlStatement = reader["SqlStatement"].ToString();
                            var connectionString = reader["ConnectionString"].ToString();
                            stepExecution = new SqlStepExecution(executionConfiguration, stepId, sqlStatement, connectionString, timeoutMinutes);
                        }
                        else if (stepType == "SSIS")
                        {
                            var connectionString = reader["ConnectionString"].ToString();
                            var packageFolderName = reader["PackageFolderName"].ToString();
                            var packageProjectName = reader["PackageProjectName"].ToString();
                            var packageName = reader["PackageName"].ToString();
                            var executeIn32BitMode = reader["ExecuteIn32BitMode"].ToString() == "1";
                            stepExecution = new PackageStepExecution(executionConfiguration, stepId, connectionString,
                                packageFolderName, packageProjectName, packageName, executeIn32BitMode, timeoutMinutes);
                        }
                        else if (stepType == "JOB")
                        {
                            var jobToExecuteId = reader["JobToExecuteId"].ToString();
                            var jobExecuteSynchronized = (bool)reader["JobExecuteSynchronized"];
                            stepExecution = new JobStepExecution(executionConfiguration, stepId, jobToExecuteId, jobExecuteSynchronized);
                        }
                        else if (stepType == "PIPELINE")
                        {
                            var dataFactoryId = reader["DataFactoryId"].ToString();
                            var pipelineName = reader["PipelineName"].ToString();
                            stepExecution = new PipelineStepExecution(executionConfiguration, stepId, dataFactoryId, pipelineName, timeoutMinutes);
                        }
                        else
                        {
                            Log.Error("{ExecutionId} {StepId} Incorrect step type {stepType}", executionConfiguration.ExecutionId, stepId, stepType);
                            return;
                        }

                    }
                    else
                    {
                        Log.Error("{ExecutionId} {StepId} Could not find execution details", executionConfiguration.ExecutionId, stepId);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error reading execution details", executionConfiguration.ExecutionId, stepId);
                    return;
                }
            }

            // Loop until there are not retry attempts left.
            while (AttemptCounter <= retryAttempts)
            {
                using (var connection = new SqlConnection(executionConfiguration.ConnectionString))
                {
                    await connection.OpenIfClosedAsync(); // Open the connection to ETL Manager database for execution start logging.
                    // In case of first execution, update the existing execution row.
                    if (AttemptCounter == 0)
                    {
                        try
                        {
                            var startUpdate = new SqlCommand(
                              @"UPDATE etlmanager.Execution
                                SET StartDateTime = GETDATE(), ExecutionStatus = 'RUNNING'
                                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                                , connection);
                            startUpdate.Parameters.AddWithValue("@ExecutionId", executionConfiguration.ExecutionId);
                            startUpdate.Parameters.AddWithValue("@StepId", stepId);
                            startUpdate.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                            await startUpdate.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "{ExecutionId} {StepId} Error updating step status to RUNNING", executionConfiguration.ExecutionId, stepId);
                        }
                    }
                    // In case of later retry execution, insert new execution row based on the first one (RetryAttemptIndex = 0).
                    else
                    {
                        try
                        {
                            var addNewExecution = new SqlCommand(
                              @"EXEC [etlmanager].[ExecutionStepCopy] @ExecutionId = @ExecutionId_, @StepId = @StepId_, @RetryAttemptIndex = @RetryAttemptIndex_"
                                , connection);
                            addNewExecution.Parameters.AddWithValue("@ExecutionId_", executionConfiguration.ExecutionId);
                            addNewExecution.Parameters.AddWithValue("@StepId_", stepId);
                            addNewExecution.Parameters.AddWithValue("@RetryAttemptIndex_", AttemptCounter);
                            await addNewExecution.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "{ExecutionId} {StepId} Error copying step execution details for retry attempt", executionConfiguration.ExecutionId, stepId);
                        }
                    }
                }

                // Execute the step based on its step type.
                ExecutionResult executionResult;
                try
                {
                    executionResult = await stepExecution.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    executionResult = new ExecutionResult.Failure("Error during step execution: " + ex.Message);
                }

                using (var connection = new SqlConnection(executionConfiguration.ConnectionString))
                {
                    await connection.OpenIfClosedAsync(); // Open the connection to ETL Manager database for execution end logging.
                    if (executionResult is ExecutionResult.Failure failureResult)
                    {
                        Log.Warning("{ExecutionId} {StepId} Error executing step: " + failureResult.ErrorMessage, executionConfiguration.ExecutionId, stepId);

                        // The step failed. Update the execution accordingly.

                        // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
                        var status = AttemptCounter >= retryAttempts ? "FAILED" : "AWAIT RETRY";

                        try
                        {
                            var errorUpdate = new SqlCommand(
                              @"UPDATE etlmanager.Execution
                                SET EndDateTime = GETDATE(), ExecutionStatus = @ExecutionStatus, ErrorMessage = @ErrorMessage, InfoMessage = @InfoMessage
                                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                                , connection);
                            errorUpdate.Parameters.AddWithValue("@ExecutionId", executionConfiguration.ExecutionId);
                            errorUpdate.Parameters.AddWithValue("@StepId", stepId);
                            errorUpdate.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                            errorUpdate.Parameters.AddWithValue("@ExecutionStatus", status);
                            errorUpdate.Parameters.AddWithValue("@ErrorMessage", failureResult.ErrorMessage);
                            errorUpdate.Parameters.AddWithValue("@InfoMessage", failureResult.InfoMessage?.Length > 0 ? (object)failureResult.InfoMessage : DBNull.Value);
                            await errorUpdate.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "{ExecutionId} {StepId} Error updating step status to {status}", executionConfiguration.ExecutionId, stepId, status);
                        }
                    }
                    else
                    {
                        Log.Information("{ExecutionId} {StepId} Step executed successfully", executionConfiguration.ExecutionId, stepId);

                        // The package was executed successfully. Update the execution accordingly.
                        try
                        {
                            var successUpdate = new SqlCommand(
                              @"UPDATE etlmanager.Execution
                                SET EndDateTime = GETDATE(), ExecutionStatus = 'COMPLETED', InfoMessage = @InfoMessage
                                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                                , connection);
                            successUpdate.Parameters.AddWithValue("@ExecutionId", executionConfiguration.ExecutionId);
                            successUpdate.Parameters.AddWithValue("@StepId", stepId);
                            successUpdate.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                            successUpdate.Parameters.AddWithValue("@InfoMessage", executionResult.InfoMessage?.Length > 0 ? (object)executionResult.InfoMessage : DBNull.Value);
                            await successUpdate.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "{ExecutionId} {StepId} Error updating step status to COMPLETED", executionConfiguration.ExecutionId, stepId);
                        }

                        break; // Break the loop to end this execution.
                    }
                }

                // The step failed. There are attempts left => increase counter and wait for the retry interval.
                if (AttemptCounter < retryAttempts)
                {
                    AttemptCounter++;
                    await Task.Delay(retryIntervalMinutes * 60 * 1000);
                }
                // Otherwise break the loop and end this execution.
                else
                {
                    break;
                }
            }
        }

    }

}
