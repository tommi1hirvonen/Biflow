using Serilog;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Threading;

namespace EtlManagerExecutor
{
    class StepWorker
    {
        public delegate void StepCompletedDelegate(ExecutionConfiguration executionConfiguration, string stepId);

        private readonly ExecutionConfiguration executionConfiguration;
        private readonly string stepId;
        private readonly StepCompletedDelegate stepCompleted;

        private StepConfiguration StepConfiguration { get; set; }

        private int AttemptCounter { get; set; } = 0;

        public StepWorker(ExecutionConfiguration executionConfiguration, string stepId, StepCompletedDelegate stepCompleted)
        {
            this.executionConfiguration = executionConfiguration;
            this.stepId = stepId;
            this.stepCompleted = stepCompleted;
        }

        public void ExecuteStep(object sender, DoWorkEventArgs args)
        {

            // Get step details.
            using (SqlConnection sqlConnection = new SqlConnection(executionConfiguration.ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(
                    @"SELECT TOP 1 StepType, SqlStatement, ConnectionId, etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionPassword) AS ConnectionString,
                        PackageFolderName, PackageProjectName, PackageName,
                        ExecuteIn32BitMode, JobToExecuteId, JobExecuteSynchronized, RetryAttempts, RetryIntervalMinutes, DataFactoryId, PipelineName
                    FROM etlmanager.Execution
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                    , sqlConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", executionConfiguration.ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", stepId);
                sqlCommand.Parameters.AddWithValue("@EncryptionPassword", executionConfiguration.EncryptionPassword);
                try
                {
                    sqlConnection.Open();
                    using var reader = sqlCommand.ExecuteReader();
                    if (reader.Read())
                    {
                        var stepType = reader["StepType"].ToString();
                        var retryAttempts = (int)reader["RetryAttempts"];
                        var retryIntervalMinutes = (int)reader["RetryIntervalMinutes"];
                        if (stepType == "SQL")
                        {
                            var sqlStatement = reader["SqlStatement"].ToString();
                            var connectionString = reader["ConnectionString"].ToString();
                            StepConfiguration = new SqlStepConfiguration(stepId, retryAttempts, retryIntervalMinutes, sqlStatement, connectionString);
                        }
                        else if (stepType == "SSIS")
                        {
                            var connectionString = reader["ConnectionString"].ToString();
                            var packageFolderName = reader["PackageFolderName"].ToString();
                            var packageProjectName = reader["PackageProjectName"].ToString();
                            var packageName = reader["PackageName"].ToString();
                            var executeIn32BitMode = reader["ExecuteIn32BitMode"].ToString() == "1";
                            StepConfiguration = new PackageStepConfiguration(stepId, retryAttempts, retryIntervalMinutes, connectionString,
                                packageFolderName, packageProjectName, packageName, executeIn32BitMode);
                        }
                        else if (stepType == "JOB")
                        {
                            var jobToExecuteId = reader["JobToExecuteId"].ToString();
                            var jobExecuteSynchronized = (bool)reader["JobExecuteSynchronized"];
                            StepConfiguration = new JobStepConfiguration(stepId, retryAttempts, retryIntervalMinutes, jobToExecuteId, jobExecuteSynchronized);
                        }
                        else if (stepType == "PIPELINE")
                        {
                            var dataFactoryId = reader["DataFactoryId"].ToString();
                            var pipelineName = reader["PipelineName"].ToString();
                            StepConfiguration = new PipelineStepConfiguration(stepId, retryAttempts, retryIntervalMinutes, dataFactoryId, pipelineName);
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
            while (AttemptCounter <= StepConfiguration.RetryAttempts)
            {
                using (SqlConnection connection = new SqlConnection(executionConfiguration.ConnectionString))
                {
                    connection.OpenIfClosed(); // Open the connection to ETL Manager database for execution start logging.
                    // In case of first execution, update the existing execution row.
                    if (AttemptCounter == 0)
                    {
                        try
                        {
                            SqlCommand startUpdate = new SqlCommand(
                              @"UPDATE etlmanager.Execution
                                SET StartDateTime = GETDATE(), ExecutionStatus = 'RUNNING'
                                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                                , connection);
                            startUpdate.Parameters.AddWithValue("@ExecutionId", executionConfiguration.ExecutionId);
                            startUpdate.Parameters.AddWithValue("@StepId", stepId);
                            startUpdate.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                            startUpdate.ExecuteNonQuery();
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
                            SqlCommand addNewExecution = new SqlCommand(
                              @"EXEC [etlmanager].[ExecutionStepCopy] @ExecutionId = @ExecutionId_, @StepId = @StepId_, @RetryAttemptIndex = @RetryAttemptIndex_"
                                , connection);
                            addNewExecution.Parameters.AddWithValue("@ExecutionId_", executionConfiguration.ExecutionId);
                            addNewExecution.Parameters.AddWithValue("@StepId_", stepId);
                            addNewExecution.Parameters.AddWithValue("@RetryAttemptIndex_", AttemptCounter);
                            addNewExecution.ExecuteNonQuery();
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
                    IStepExecution stepExecution = StepConfiguration switch
                    {
                        SqlStepConfiguration sql => new SqlStepExecution(executionConfiguration, sql),
                        PackageStepConfiguration package => new PackageStepExecution(executionConfiguration, package, AttemptCounter),
                        PipelineStepConfiguration pipeline => new PipelineStepExecution(executionConfiguration, pipeline, AttemptCounter),
                        JobStepConfiguration job => new JobStepExecution(executionConfiguration, job),
                        _ => throw new InvalidEnumArgumentException("Unrecognized step type")
                    };
                    executionResult = stepExecution.Run();
                }
                catch (Exception ex)
                {
                    executionResult = new ExecutionResult.Failure("Error during step execution: " + ex.Message);
                }

                using (SqlConnection connection = new SqlConnection(executionConfiguration.ConnectionString))
                {
                    connection.OpenIfClosed(); // Open the connection to ETL Manager database for execution end logging.
                    if (executionResult is ExecutionResult.Failure failureResult)
                    {
                        Log.Warning("{ExecutionId} {StepId} Error executing step: " + failureResult.ErrorMessage, executionConfiguration.ExecutionId, stepId);

                        // The step failed. Update the execution accordingly.

                        // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
                        var status = AttemptCounter >= StepConfiguration.RetryAttempts ? "FAILED" : "AWAIT RETRY";

                        try
                        {
                            SqlCommand errorUpdate = new SqlCommand(
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
                            errorUpdate.ExecuteNonQuery();
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
                            SqlCommand successUpdate = new SqlCommand(
                              @"UPDATE etlmanager.Execution
                                SET EndDateTime = GETDATE(), ExecutionStatus = 'COMPLETED', InfoMessage = @InfoMessage
                                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                                , connection);
                            successUpdate.Parameters.AddWithValue("@ExecutionId", executionConfiguration.ExecutionId);
                            successUpdate.Parameters.AddWithValue("@StepId", stepId);
                            successUpdate.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                            successUpdate.Parameters.AddWithValue("@InfoMessage", executionResult.InfoMessage?.Length > 0 ? (object)executionResult.InfoMessage : DBNull.Value);
                            successUpdate.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "{ExecutionId} {StepId} Error updating step status to COMPLETED", executionConfiguration.ExecutionId, stepId);
                        }

                        break; // Break the loop to end this execution.
                    }
                }

                // The step failed. There are attempts left => increase counter and wait for the retry interval.
                if (AttemptCounter < StepConfiguration.RetryAttempts)
                {
                    AttemptCounter++;
                    Thread.Sleep(StepConfiguration.RetryIntervalMinutes * 60 * 1000);
                }
                // Otherwise break the loop and end this execution.
                else
                {
                    break;
                }
            }
        }

        public void OnStepCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            stepCompleted(executionConfiguration, stepId);
        }

    }

}
