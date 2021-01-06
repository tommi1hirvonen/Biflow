using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class PackageStepExecution : IStepExecution
    {
        private readonly ExecutionConfiguration executionConfig;
        private readonly PackageStepConfiguration packageStep;
        private readonly int retryAttempt;

        private bool Completed { get; set; }
        private bool Success { get; set; }

        private const int MaxRefreshRetries = 5;

        public PackageStepExecution(ExecutionConfiguration executionConfiguration, PackageStepConfiguration packageStepConfiguration, int retryAttempt)
        {
            executionConfig = executionConfiguration;
            packageStep = packageStepConfiguration;
            this.retryAttempt = retryAttempt;
        }

        public async Task<ExecutionResult> RunAsync()
        {
            // Get possible parameters.
            var parameters = new Dictionary<string, string>();

            // Connect to ETL Manager database.
            using (var sqlConnection = new SqlConnection(executionConfig.ConnectionString))
            {
                var paramsCommand = new SqlCommand("SELECT [ParameterName], [ParameterValue] FROM [etlmanager].[Parameter] WHERE StepId = @StepId"
                    , sqlConnection);
                paramsCommand.Parameters.AddWithValue("@StepId", packageStep.StepId);

                try
                {
                    Log.Information("{ExecutionId} {StepId} Retrieving package parameters", executionConfig.ExecutionId, packageStep.StepId);

                    await sqlConnection.OpenIfClosedAsync();
                    using var reader = await paramsCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        parameters.Add(reader["ParameterName"].ToString(), reader["ParameterValue"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error retrieving package parameters", executionConfig.ExecutionId, packageStep.StepId);
                    return new ExecutionResult.Failure("Error reading package parameters: " + ex.Message);
                }

            }


            // Start the package execution and capture the SSISDB operation id.
            long operationId;
            try
            {
                Log.Information("{ExecutionId} {StepId} Starting package execution", executionConfig.ExecutionId, packageStep.StepId);

                operationId = await StartExecutionAsync(parameters);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error executing package", executionConfig.ExecutionId, packageStep.StepId);
                return new ExecutionResult.Failure("Error executing package: " + ex.Message);
            }

            // Update the SSISDB operation id for the target package execution.
            try
            {
                using var etlManagerConnection = new SqlConnection(executionConfig.ConnectionString);
                await etlManagerConnection.OpenIfClosedAsync();
                var sqlCommand = new SqlCommand(
                    @"UPDATE etlmanager.Execution
                        SET PackageOperationId = @OperationId
                        WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex"
                    , etlManagerConnection);
                sqlCommand.Parameters.AddWithValue("@ExecutionId", executionConfig.ExecutionId);
                sqlCommand.Parameters.AddWithValue("@StepId", packageStep.StepId);
                sqlCommand.Parameters.AddWithValue("@RetryAttemptIndex", retryAttempt);
                sqlCommand.Parameters.AddWithValue("@OperationId", operationId);
                await sqlCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error updating target package operation id (" + operationId + ")");
            }

            // Monitor the package's execution.
            try
            {
                await TryRefreshStatusAsync(operationId);
                while (!Completed)
                {
                    await Task.Delay(executionConfig.PollingIntervalMs);
                    await TryRefreshStatusAsync(operationId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {StepId} Error monitoring package execution status", executionConfig.ExecutionId, packageStep.StepId);
                return new ExecutionResult.Failure("Error monitoring package execution status: " + ex.Message);
            }

            // The package has completed. If the package failed, retrieve error messages.
            if (!Success)
            {
                try
                {
                    List<string> errors = await GetErrorMessagesAsync(operationId);
                    return new ExecutionResult.Failure(string.Join("\n\n", errors));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {StepId} Error getting package error messages", executionConfig.ExecutionId, packageStep.StepId);
                    return new ExecutionResult.Failure("Error getting package error messages: " + ex.Message);
                }

            }

            return new ExecutionResult.Success();
        }

        public async Task<long> StartExecutionAsync(Dictionary<string, string> parameters)
        {
            using var sqlConnection = new SqlConnection(packageStep.ConnectionString);
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

            executionCommand.Parameters.AddWithValue("@FolderName", packageStep.FolderName);
            executionCommand.Parameters.AddWithValue("@ProjectName", packageStep.ProjectName);
            executionCommand.Parameters.AddWithValue("@PackageName", packageStep.PackageName);
            executionCommand.Parameters.AddWithValue("@ExecuteIn32BitMode", packageStep.ExecuteIn32BitMode ? 1 : 0);

            foreach (var parameter in parameters)
            {
                executionCommand.Parameters.AddWithValue("@ParameterName" + parameter.Key, parameter.Key);
                executionCommand.Parameters.AddWithValue("@ParameterValue" + parameter.Key, parameter.Value);
            }

            await sqlConnection.OpenAsync();

            var operationId = (long)await executionCommand.ExecuteScalarAsync();
            return operationId;
        }

        public async Task TryRefreshStatusAsync(long operationId)
        {
            int refreshRetries = 0;
            // Try to refresh the operation status until the maximum number of attempts is reached.
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    using var sqlConnection = new SqlConnection(packageStep.ConnectionString);
                    var sqlCommand = new SqlCommand("SELECT status from SSISDB.catalog.operations where operation_id = @OperationId", sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@OperationId", operationId);
                    await sqlConnection.OpenAsync();
                    int status = (int)await sqlCommand.ExecuteScalarAsync();
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
                    Log.Error(ex, "Error refreshing package operation status for operation id {operationId}", operationId);
                    refreshRetries++;
                    await Task.Delay(executionConfig.PollingIntervalMs);
                }
            }
            // The maximum number of attempts was reached. Notify caller with exception.
            throw new TimeoutException("The maximum number of package operation status refresh attempts was reached.");
        }

        public async Task<List<string>> GetErrorMessagesAsync(long operationId)
        {
            using var sqlConnection = new SqlConnection(packageStep.ConnectionString);
            var sqlCommand = new SqlCommand(
                @"SELECT message
                FROM SSISDB.catalog.operation_messages
                WHERE message_type = 120 AND operation_id = @OperationId" // message_type = 120 => error message
                , sqlConnection);
            sqlCommand.Parameters.AddWithValue("@OperationId", operationId);
            await sqlConnection.OpenAsync();
            var messages = new List<string>();
            var reader = await sqlCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                messages.Add(reader[0].ToString());
            }
            return messages;
        }
    }
}
