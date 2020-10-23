using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace EtlManagerExecutor
{
    class StepWorker
    {
        public delegate void StepCompletedDelegate(string stepId);

        readonly string ExecutionId;
        readonly string StepId;
        readonly StepCompletedDelegate StepCompleted;
        readonly string EtlManagerConnectionString;
        readonly int PollingIntervalMs;

        private string SqlStatement { get; set; }

        private string ServerNamePackage { get; set; }
        private string FolderName { get; set; }
        private string ProjectName { get; set; }
        private string PackageName { get; set; }
        private bool ExecuteIn32BitMode { get; set; } = false;

        private int RetryAttempts { get; set; } = 0;
        private int RetryIntervalMinutes { get; set; } = 0;

        private int AttemptCounter { get; set; } = 0;

        public StepWorker(string executionId, string stepId, string connectionString, int pollingIntervalMs, StepCompletedDelegate stepCompleted)
        {
            ExecutionId = executionId;
            StepId = stepId;
            StepCompleted = stepCompleted;
            EtlManagerConnectionString = connectionString;
            PollingIntervalMs = pollingIntervalMs;
        }

        public void ExecuteStep(object sender, DoWorkEventArgs args)
        {
            string stepType = string.Empty;

            // Get step details.
            using (SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(
                    @"SELECT TOP 1 StepType, SqlStatement, ServerName, FolderName, ProjectName, PackageName, ExecuteIn32BitMode, RetryAttempts, RetryIntervalMinutes
                    FROM etlmanager.Execution
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                    , sqlConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", StepId);
                try
                {
                    sqlConnection.Open();
                    using var reader = sqlCommand.ExecuteReader();
                    if (reader.Read())
                    {
                        stepType = reader["StepType"].ToString();
                        if (stepType == "SQL")
                        {
                            SqlStatement = reader["SqlStatement"].ToString();
                        }
                        else if (stepType == "SSIS")
                        {
                            ServerNamePackage = reader["ServerName"].ToString();
                            FolderName = reader["FolderName"].ToString();
                            ProjectName = reader["ProjectName"].ToString();
                            PackageName = reader["PackageName"].ToString();
                            ExecuteIn32BitMode = reader["ExecuteIn32BitMode"].ToString() == "1";
                        }
                        RetryAttempts = (int)reader["RetryAttempts"];
                        RetryIntervalMinutes = (int)reader["RetryIntervalMinutes"];
                    }
                    else
                    {
                        Log.Error("{ExecutionId} {StepId} Could not find execution details", ExecutionId, StepId);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error reading execution details", ExecutionId, StepId);
                    return;
                }
            }

            // Loop until there are not retry attempts left.
            while (AttemptCounter <= RetryAttempts)
            {
                using (SqlConnection connection = new SqlConnection(EtlManagerConnectionString))
                {
                    connection.OpenIfClosed(); // Open the connection to ETL Manager database for execution start logging.
                    // In case of first execution, update the existing execution row.
                    if (AttemptCounter == 0)
                    {
                        SqlCommand startUpdate = new SqlCommand(
                          @"UPDATE etlmanager.Execution
                            SET StartDateTime = GETDATE(), ExecutionStatus = 'RUNNING'
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                            , connection);
                        startUpdate.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                        startUpdate.Parameters.AddWithValue("@StepId", StepId);
                        startUpdate.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                        startUpdate.ExecuteNonQuery();
                    }
                    // In case of later retry execution, insert new execution row based on the first one (RetryAttemptIndex = 0).
                    else
                    {
                        SqlCommand addNewExecution = new SqlCommand(
                          @"INSERT INTO etlmanager.Execution (ExecutionId, JobId, JobName, StepId, StepName, CreatedDateTime, RetryAttemptIndex, StartDateTime, ExecutionStatus, RetryAttempts, RetryIntervalMinutes, ExecutionPhase, DependencyMode, StepType, SqlStatement, ServerName, FolderName, ProjectName, PackageName, ExecuteIn32BitMode, CreatedBy, JobExecutorOperationId, StepExecutorOperationId, ScheduleId)
                            SELECT ExecutionId, JobId, JobName, StepId, StepName, CreatedDateTime, @RetryAttemptIndex, StartDateTime = GETDATE(), ExecutionStatus = 'RUNNING', RetryAttempts, RetryIntervalMinutes, ExecutionPhase, DependencyMode, StepType, SqlStatement, ServerName, FolderName, ProjectName, PackageName, ExecuteIn32BitMode, CreatedBy, JobExecutorOperationId, StepExecutorOperationId, ScheduleId
                            FROM etlmanager.Execution
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = 0"
                            , connection);
                        addNewExecution.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                        addNewExecution.Parameters.AddWithValue("@StepId", StepId);
                        addNewExecution.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                        addNewExecution.ExecuteNonQuery();
                    }
                }

                ExecutionResult executionResult;
                // Execute the step based on its step type.
                if (stepType == "SQL")
                {
                    executionResult = StartSqlExecution();
                }
                else if (stepType == "SSIS")
                {
                    executionResult = StartPackageExecution();
                }
                else
                {
                    executionResult = new ExecutionResult.Failure("Incorrect step type");
                }

                using (SqlConnection connection = new SqlConnection(EtlManagerConnectionString))
                {
                    connection.OpenIfClosed(); // Open the connection to ETL Manager database for execution end logging.
                    if (executionResult is ExecutionResult.Failure failureResult)
                    {
                        Log.Warning("{ExecutionId} {StepId} Error executing step: " + failureResult.ErrorMessage, ExecutionId, StepId);

                        // The step failed. Update the execution accordingly.
                        SqlCommand errorUpdate = new SqlCommand(
                          @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = @ExecutionStatus, ErrorMessage = @ErrorMessage
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                            , connection);
                        errorUpdate.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                        errorUpdate.Parameters.AddWithValue("@StepId", StepId);
                        errorUpdate.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                        // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
                        errorUpdate.Parameters.AddWithValue("@ExecutionStatus", AttemptCounter >= RetryAttempts ? "FAILED" : "AWAIT RETRY");
                        errorUpdate.Parameters.AddWithValue("@ErrorMessage", failureResult.ErrorMessage);
                        errorUpdate.ExecuteNonQuery();
                    }
                    else
                    {
                        Log.Information("{ExecutionId} {StepId} Step executed successfully", ExecutionId, StepId);

                        // The package was executed successfully. Update the execution accordingly.
                        SqlCommand successUpdate = new SqlCommand(
                          @"UPDATE etlmanager.Execution
                            SET EndDateTime = GETDATE(), ExecutionStatus = 'COMPLETED'
                            WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                            , connection);
                        successUpdate.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                        successUpdate.Parameters.AddWithValue("@StepId", StepId);
                        successUpdate.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                        successUpdate.ExecuteNonQuery();
                        break; // Break the loop to end this execution.
                    }
                }

                // The step failed. There are attempts left => increase counter and wait for the retry interval.
                if (AttemptCounter < RetryAttempts)
                {
                    AttemptCounter++;
                    Thread.Sleep(RetryIntervalMinutes * 60 * 1000);
                }
                // Otherwise break the loop and end this execution.
                else
                {
                    break;
                }
            }
        }

        

        private ExecutionResult StartPackageExecution()
        {
            Log.Information("{ExecutionId} {StepId} Started building execution for package " + FolderName + "\\" + ProjectName + "\\" + PackageName, ExecutionId, StepId);

            // Get possible parameters.
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            // Connect to ETL Manager database.
            using (SqlConnection sqlConnection = new SqlConnection(EtlManagerConnectionString))
            {
                SqlCommand paramsCommand = new SqlCommand("SELECT [ParameterName], [ParameterValue] FROM [etlmanager].[Parameter] WHERE StepId = @StepId"
                    , sqlConnection);
                paramsCommand.Parameters.AddWithValue("@StepId", StepId);

                try
                {
                    Log.Information("{ExecutionId} {StepId} Retrieving package parameters", ExecutionId, StepId);

                    sqlConnection.OpenIfClosed();
                    using SqlDataReader reader = paramsCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        parameters.Add(reader["ParameterName"].ToString(), reader["ParameterValue"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error retrieving package parameters", ExecutionId, StepId);
                    return new ExecutionResult.Failure("Error reading package parameters: " + ex.Message);
                }

            }

            PackageExecution packageExecution = new PackageExecution(ServerNamePackage, FolderName, ProjectName, PackageName, ExecuteIn32BitMode)
            {
                Parameters = parameters
            };

            // Start the package execution and capture the SSISDB operation id.
            long operationId;
            try
            {
                Log.Information("{ExecutionId} {StepId} Starting package executio", ExecutionId, StepId);

                operationId = packageExecution.StartExecution();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error executing package", ExecutionId, StepId);
                return new ExecutionResult.Failure("Error executing package: " + ex.Message);
            }

            // Update the SSISDB operation id for the target package execution.
            try
            {
                using SqlConnection etlManagerConnection = new SqlConnection(EtlManagerConnectionString);
                etlManagerConnection.OpenIfClosed();
                SqlCommand sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                        SET PackageOperationId = @OperationId
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                    , etlManagerConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", StepId);
                sqlCommand.Parameters.AddWithValue("@RetryAttemptIndex", AttemptCounter);
                sqlCommand.Parameters.AddWithValue("@OperationId", operationId);
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error updating target package operation id (" + operationId + ")");
            }

            // Monitor the package's execution.
            try
            {
                packageExecution.RefreshStatus();
                while (!packageExecution.Completed)
                {
                    Thread.Sleep(PollingIntervalMs);
                    packageExecution.RefreshStatus();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error monitoring package execution status", ExecutionId, StepId);
                return new ExecutionResult.Failure("Error monitoring package execution status: " + ex.Message);
            }

            // The package has completed. If the package failed, retrieve error messages.
            if (!packageExecution.Success)
            {
                try
                {
                    return new ExecutionResult.Failure(string.Join("\n\n", packageExecution.GetErrorMessages()));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error getting package error messages", ExecutionId, StepId);
                    return new ExecutionResult.Failure("Error getting package error messages: " + ex.Message);
                }

            }

            return new ExecutionResult.Success();
        }


        private ExecutionResult StartSqlExecution()
        {
            // Try executing the SQL statement of this step.
            try
            {
                Log.Information("{ExecutionId} {StepId} Starting SQL execution", ExecutionId, StepId);
                using SqlConnection connection = new SqlConnection(EtlManagerConnectionString);
                connection.OpenIfClosed();
                SqlCommand sqlCommand = new SqlCommand(SqlStatement, connection) { CommandTimeout = 0 }; // CommandTimeout = 0 => wait indefinitely
                sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Log.Warning(ex, "{ExecutionId} {StepId} SQL execution failed", ExecutionId, StepId);
                var errors = ex.Errors.Cast<SqlError>();
                var errorMessage = string.Join("\n\n", errors.Select(error => "Line: " + error.LineNumber + "\nMessage: " + error.Message));
                return new ExecutionResult.Failure(errorMessage);
            }

            return new ExecutionResult.Success();
        }

        public void OnStepCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            StepCompleted(StepId);
        }

    }

    public abstract class ExecutionResult
    {
        public class Success : ExecutionResult { }
        public class Failure : ExecutionResult
        {
            public string ErrorMessage { get; } = string.Empty;
            public Failure(string errorMessage)
            {
                ErrorMessage = errorMessage;
            }
        }
    }
}
