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
    class PackageStepExecution : IExecutable
    {
        private ExecutionConfiguration Configuration { get; init; }
        private string StepId { get; init; }
        private string ConnectionString { get; init; }
        public int RetryAttemptCounter { get; set; }
        private long PackageOperationId { get; set; }
        private string FolderName { get; init; }
        private string ProjectName { get; init; }
        private string PackageName { get; init; }
        private bool ExecuteIn32BitMode { get; init; }

        private int TimeoutMinutes { get; init; }
        private bool Completed { get; set; }
        private bool Success { get; set; }

        private const int MaxRefreshRetries = 3;

        public PackageStepExecution(ExecutionConfiguration configuration, string stepId, string connectionString,
            string folderName, string projectName, string packageName, bool executeIn32BitMode, int timeoutMinutes)
        {
            Configuration = configuration;
            StepId = stepId;
            ConnectionString = connectionString;
            FolderName = folderName;
            ProjectName = projectName;
            PackageName = packageName;
            ExecuteIn32BitMode = executeIn32BitMode;
            TimeoutMinutes = timeoutMinutes;
        }

        public async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get possible parameters.
            var parameters = new Dictionary<string, string>();

            // Connect to ETL Manager database.
            using (var sqlConnection = new SqlConnection(Configuration.ConnectionString))
            {
                var paramsCommand = new SqlCommand(
                    @"SELECT [ParameterName], [ParameterValue]
                    FROM [etlmanager].[ExecutionParameter]
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                    , sqlConnection);
                paramsCommand.Parameters.AddWithValue("@StepId", StepId);
                paramsCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);

                try
                {
                    Log.Information("{ExecutionId} {StepId} Retrieving package parameters", Configuration.ExecutionId, StepId);

                    await sqlConnection.OpenIfClosedAsync();
                    using var reader = await paramsCommand.ExecuteReaderAsync(CancellationToken.None);
                    while (await reader.ReadAsync(CancellationToken.None))
                    {
                        parameters.Add(reader["ParameterName"].ToString(), reader["ParameterValue"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error retrieving package parameters", Configuration.ExecutionId, StepId);
                    return new ExecutionResult.Failure("Error reading package parameters: " + ex.Message);
                }

            }


            // Start the package execution and capture the SSISDB operation id.
            DateTime startTime;
            try
            {
                Log.Information("{ExecutionId} {StepId} Starting package execution", Configuration.ExecutionId, StepId);

                await StartExecutionAsync(parameters);
                startTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error executing package", Configuration.ExecutionId, StepId);
                return new ExecutionResult.Failure("Error executing package: " + ex.Message);
            }

            // Update the SSISDB operation id for the target package execution.
            try
            {
                using var etlManagerConnection = new SqlConnection(Configuration.ConnectionString);
                await etlManagerConnection.OpenIfClosedAsync();
                var sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                        SET PackageOperationId = @OperationId
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                    , etlManagerConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", StepId);
                sqlCommand.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptCounter);
                sqlCommand.Parameters.AddWithValue("@OperationId", PackageOperationId);
                await sqlCommand.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error updating target package operation id (" + PackageOperationId + ")");
            }

            // Monitor the package's execution.
            try
            {
                await TryRefreshStatusAsync(cancellationToken);
                while (!Completed)
                {
                    // Check for possible timeout.
                    if (TimeoutMinutes > 0 && (DateTime.Now - startTime).TotalMinutes > TimeoutMinutes)
                    {
                        await CancelAsync();
                        Log.Warning("{ExecutionId} {StepId} Step execution timed out", Configuration.ExecutionId, StepId);
                        return new ExecutionResult.Failure("Step execution timed out");
                    }

                    await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
                    await TryRefreshStatusAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                await CancelAsync();
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error monitoring package execution status", Configuration.ExecutionId, StepId);
                return new ExecutionResult.Failure("Error monitoring package execution status: " + ex.Message);
            }

            // The package has completed. If the package failed, retrieve error messages.
            if (!Success)
            {
                try
                {
                    List<string> errors = await GetErrorMessagesAsync();
                    return new ExecutionResult.Failure(string.Join("\n\n", errors));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error getting package error messages", Configuration.ExecutionId, StepId);
                    return new ExecutionResult.Failure("Error getting package error messages: " + ex.Message);
                }

            }

            return new ExecutionResult.Success();
        }

        private async Task StartExecutionAsync(Dictionary<string, string> parameters)
        {
            using var sqlConnection = new SqlConnection(ConnectionString);
            var commandBuilder = new StringBuilder();
            commandBuilder.Append(
                @"DECLARE @execution_id BIGINT

                EXEC [SSISDB].[catalog].[create_execution]
                    @package_name = @PackageName,
                    @execution_id = @execution_id OUTPUT,
                    @folder_name = @FolderName,
                    @project_name = @ProjectName,
                    @use32bitruntime = @ExecuteIn32BitMode,
                    @reference_id = NULL

                EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                    @execution_id,
                    @object_type = 50,
                    @parameter_name = N'LOGGING_LEVEL',
                    @parameter_value = 1

                EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                    @execution_id,
                    @object_type = 50,
                    @parameter_name = N'SYNCHRONIZED',
                    @parameter_value = 0" + "\n"
                );

            foreach (var parameter in parameters)
            {
                commandBuilder.Append(
                    @"EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                        @execution_id,
                        @object_type = 30,
                        @parameter_name = @ParameterName" + parameter.Key + @",
                        @parameter_value = @ParameterValue" + parameter.Key + "\n"
                    );
            }

            commandBuilder.Append(
                @"EXEC [SSISDB].[catalog].[start_execution] @execution_id

                SELECT @execution_id"
                );
            string commandString = commandBuilder.ToString();
            var executionCommand = new SqlCommand(commandString, sqlConnection);

            executionCommand.Parameters.AddWithValue("@FolderName", FolderName);
            executionCommand.Parameters.AddWithValue("@ProjectName", ProjectName);
            executionCommand.Parameters.AddWithValue("@PackageName", PackageName);
            executionCommand.Parameters.AddWithValue("@ExecuteIn32BitMode", ExecuteIn32BitMode ? 1 : 0);

            foreach (var parameter in parameters)
            {
                executionCommand.Parameters.AddWithValue("@ParameterName" + parameter.Key, parameter.Key);
                executionCommand.Parameters.AddWithValue("@ParameterValue" + parameter.Key, parameter.Value);
            }

            await sqlConnection.OpenAsync();

            PackageOperationId = (long)await executionCommand.ExecuteScalarAsync();
        }

        private async Task TryRefreshStatusAsync(CancellationToken cancellationToken)
        {
            int refreshRetries = 0;
            // Try to refresh the operation status until the maximum number of attempts is reached.
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    using var sqlConnection = new SqlConnection(ConnectionString);
                    var sqlCommand = new SqlCommand("SELECT status from SSISDB.catalog.operations where operation_id = @OperationId", sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@OperationId", PackageOperationId);
                    await sqlConnection.OpenAsync(CancellationToken.None);
                    int status = (int)await sqlCommand.ExecuteScalarAsync(CancellationToken.None);
                    // created (1), running (2), canceled (3), failed (4), pending (5), ended unexpectedly (6), succeeded (7), stopping (8), completed (9)
                    if (status == 3 || status == 4 || status == 6 || status == 7 || status == 9)
                    {
                        Completed = true;
                        if (status == 7) Success = true;
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error refreshing package operation status for operation id {operationId}", PackageOperationId);
                    refreshRetries++;
                    await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
                }
            }
            // The maximum number of attempts was reached. Notify caller with exception.
            throw new TimeoutException("The maximum number of package operation status refresh attempts was reached.");
        }

        private async Task<List<string>> GetErrorMessagesAsync()
        {
            using var sqlConnection = new SqlConnection(ConnectionString);
            var sqlCommand = new SqlCommand(
                @"SELECT message
                FROM SSISDB.catalog.operation_messages
                WHERE message_type = 120 AND operation_id = @OperationId" // message_type = 120 => error message
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@OperationId", PackageOperationId);
            await sqlConnection.OpenAsync();
            var messages = new List<string>();
            var reader = await sqlCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                messages.Add(reader[0].ToString());
            }
            return messages;
        }

        public async Task<bool> CancelAsync()
        {
            Log.Information("{ExecutionId} {StepId} Stopping package operation id {PackageOperationId}", Configuration.ExecutionId, StepId, PackageOperationId);
            try
            {
                using var sqlConnection = new SqlConnection(ConnectionString);
                await sqlConnection.OpenAsync();
                var stopPackageOperationCmd = new SqlCommand("EXEC SSISDB.catalog.stop_operation @OperationId", sqlConnection) { CommandTimeout = 60 }; // One minute
                stopPackageOperationCmd.Parameters.AddWithValue("@OperationId", PackageOperationId);
                await stopPackageOperationCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error stopping package operation id {operationId}", Configuration.ExecutionId, StepId, PackageOperationId);
                return false;
            }

            try
            {
                using SqlConnection sqlConnection = new SqlConnection(Configuration.ConnectionString);
                await sqlConnection.OpenAsync();
                SqlCommand updateStatus = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                    SET EndDateTime = GETDATE(),
                        StartDateTime = ISNULL(StartDateTime, GETDATE()),
	                    ExecutionStatus = 'STOPPED',
                        StoppedBy = @Username
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex AND EndDateTime IS NULL"
                    , sqlConnection);
                updateStatus.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);
                updateStatus.Parameters.AddWithValue("@StepId", StepId);
                updateStatus.Parameters.AddWithValue("@RetryAttemptIndex", RetryAttemptCounter);

                if (Configuration.Username is not null) updateStatus.Parameters.AddWithValue("@Username", Configuration.Username);
                else updateStatus.Parameters.AddWithValue("@Username", DBNull.Value);

                await updateStatus.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error logging SSIS step as stopped", Configuration.ExecutionId, StepId);
                return false;
            }
            Log.Information("{ExecutionId} {StepId} Successfully stopped package operation id {PackageOperationId}", Configuration.ExecutionId, StepId, PackageOperationId);
            return true;
        }
    }
}
