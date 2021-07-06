using Dapper;
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
    class PackageStepExecutionBuilder : IStepExecutionBuilder
    {
        public async Task<StepExecutionBase> CreateAsync(ExecutionConfiguration config, Step step, SqlConnection sqlConnection)
        {
            
            (var packageFolderName, var packageProjectName, var packageName, var executeIn32BitMode,
                var executeAsLogin, var executePackagesAsLogin, var connectionString, var timeoutMinutes) =
                await sqlConnection.QueryFirstAsync<(string, string, string, bool, string?, string?, string, int)>(
                    @"SELECT TOP 1
                        PackageFolderName,
                        PackageProjectName,
                        PackageName,
                        ExecuteIn32BitMode,
                        ExecuteAsLogin,
                        ExecutePackagesAsLogin = etlmanager.GetExecutePackagesAsLogin(ConnectionId),
                        ConnectionString = etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionPassword),
                        TimeoutMinutes
                    FROM etlmanager.Execution with (nolock)
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId",
                    new { config.ExecutionId, step.StepId, EncryptionPassword = config.EncryptionKey });
            executeAsLogin = string.IsNullOrWhiteSpace(executeAsLogin) ? null : executeAsLogin;
            executePackagesAsLogin = string.IsNullOrWhiteSpace(executePackagesAsLogin) ? null : executePackagesAsLogin;
            executeAsLogin ??= executePackagesAsLogin;
            return new PackageStepExecution(config, step, connectionString,
                    packageFolderName, packageProjectName, packageName, executeIn32BitMode, timeoutMinutes, executeAsLogin);
        }
    }

    class PackageStepExecution : StepExecutionBase
    {
        private string ConnectionString { get; init; }
        private long PackageOperationId { get; set; }
        private string FolderName { get; init; }
        private string ProjectName { get; init; }
        private string PackageName { get; init; }
        private bool ExecuteIn32BitMode { get; init; }
        private string? ExecuteAsLogin { get; set; }

        private int TimeoutMinutes { get; init; }
        private bool Completed { get; set; }
        private bool Success { get; set; }

        private const int MaxRefreshRetries = 3;

        public PackageStepExecution(ExecutionConfiguration configuration, Step step, string connectionString,
            string folderName, string projectName, string packageName, bool executeIn32BitMode, int timeoutMinutes, string? executeAsLogin)
            : base(configuration, step)
        {
            ConnectionString = connectionString;
            FolderName = folderName;
            ProjectName = projectName;
            PackageName = packageName;
            ExecuteIn32BitMode = executeIn32BitMode;
            TimeoutMinutes = timeoutMinutes;
            ExecuteAsLogin = executeAsLogin;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get possible parameters.
            HashSet<Parameter> parameters;
            try
            {
                parameters = await GetStepParameters();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error retrieving package parameters", Configuration.ExecutionId, Step);
                return new ExecutionResult.Failure("Error reading package parameters: " + ex.Message);
            }

            // Start the package execution and capture the SSISDB operation id.
            DateTime startTime;
            try
            {
                Log.Information("{ExecutionId} {Step} Starting package execution", Configuration.ExecutionId, Step);

                await StartExecutionAsync(parameters);
                startTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error executing package", Configuration.ExecutionId, Step);
                return new ExecutionResult.Failure("Error executing package: " + ex.Message);
            }

            // Update the SSISDB operation id for the target package execution.
            try
            {
                using var etlManagerConnection = new SqlConnection(Configuration.ConnectionString);
                await etlManagerConnection.ExecuteAsync(
                    @"UPDATE etlmanager.Execution
                    SET PackageOperationId = @OperationId
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND RetryAttemptIndex = @RetryAttemptIndex",
                    new { Configuration.ExecutionId, Step.StepId, RetryAttemptIndex = RetryAttemptCounter, OperationId = PackageOperationId });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating target package operation id (" + PackageOperationId + ")", Configuration.ExecutionId, Step);
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
                        Log.Warning("{ExecutionId} {Step} Step execution timed out", Configuration.ExecutionId, Step);
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
                Log.Error(ex, "{ExecutionId} {Step} Error monitoring package execution status", Configuration.ExecutionId, Step);
                return new ExecutionResult.Failure("Error monitoring package execution status: " + ex.Message);
            }

            // The package has completed. If the package failed, retrieve error messages.
            if (!Success)
            {
                try
                {
                    List<string?> errors = await GetErrorMessagesAsync();
                    return new ExecutionResult.Failure(string.Join("\n\n", errors));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error getting package error messages", Configuration.ExecutionId, Step);
                    return new ExecutionResult.Failure("Error getting package error messages: " + ex.Message);
                }

            }

            return new ExecutionResult.Success();
        }

        private async Task StartExecutionAsync(HashSet<Parameter> parameters)
        {
            using var sqlConnection = new SqlConnection(ConnectionString);
            var commandBuilder = new StringBuilder();
            commandBuilder.Append("USE SSISDB\n");
            if (ExecuteAsLogin is not null)
            {
                commandBuilder.Append("EXECUTE AS LOGIN = @ExecuteAsLogin\n");
            }
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
                var objectType = parameter.Level == "Project" ? "20" : "30"; // 20 => project parameter; 30 => package parameter
                // Same parameter name can be used for project and package parameter.
                // Use level in addition to name to uniquely identify each parameter.
                commandBuilder.Append(
                    @"EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                        @execution_id,
                        @object_type = " + objectType + @",
                        @parameter_name = @ParameterName" + parameter.Name + parameter.Level + @",
                        @parameter_value = @ParameterValue" + parameter.Name + parameter.Level + "\n"
                    );
            }

            commandBuilder.Append(
                @"EXEC [SSISDB].[catalog].[start_execution] @execution_id

                SELECT @execution_id"
                );
            string commandString = commandBuilder.ToString();
            using var executionCommand = new SqlCommand(commandString, sqlConnection);

            if (ExecuteAsLogin is not null)
            {
                executionCommand.Parameters.AddWithValue("@ExecuteAsLogin", ExecuteAsLogin);
            }
            executionCommand.Parameters.AddWithValue("@FolderName", FolderName);
            executionCommand.Parameters.AddWithValue("@ProjectName", ProjectName);
            executionCommand.Parameters.AddWithValue("@PackageName", PackageName);
            executionCommand.Parameters.AddWithValue("@ExecuteIn32BitMode", ExecuteIn32BitMode ? 1 : 0);

            foreach (var parameter in parameters)
            {
                executionCommand.Parameters.AddWithValue("@ParameterName" + parameter.Name + parameter.Level, parameter.Name);
                executionCommand.Parameters.AddWithValue("@ParameterValue" + parameter.Name + parameter.Level, parameter.Value);
            }

            await sqlConnection.OpenAsync(CancellationToken.None);

            PackageOperationId = (long)(await executionCommand.ExecuteScalarAsync())!;
        }

        private async Task<HashSet<Parameter>> GetStepParameters()
        {
            using var sqlConnection = new SqlConnection(Configuration.ConnectionString);
            var rows = await sqlConnection.QueryAsync<Parameter>(
                @"SELECT [Name] = [ParameterName], [Value] = [ParameterValue], [Level] = [ParameterLevel]
                FROM etlmanager.ExecutionParameter
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId AND ParameterLevel IN ('Package','Project')",
                new { Step.StepId, Configuration.ExecutionId });
            var parameters = rows.ToHashSet();
            return parameters;
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
                    var status = await sqlConnection.ExecuteScalarAsync<int>(
                        "SELECT status from SSISDB.catalog.operations where operation_id = @OperationId",
                        new { OperationId = PackageOperationId });
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

        private async Task<List<string?>> GetErrorMessagesAsync()
        {
            using var sqlConnection = new SqlConnection(ConnectionString);
            var messages = await sqlConnection.QueryAsync<string?>(
                @"SELECT message
                FROM SSISDB.catalog.operation_messages
                WHERE message_type = 120 AND operation_id = @OperationId", // message_type = 120 => error message
                new { OperationId = PackageOperationId });
            return messages.ToList();
        }

        private async Task CancelAsync()
        {
            Log.Information("{ExecutionId} {Step} Stopping package operation id {PackageOperationId}", Configuration.ExecutionId, Step, PackageOperationId);
            try
            {
                using var sqlConnection = new SqlConnection(ConnectionString);
                await sqlConnection.ExecuteAsync("EXEC SSISDB.catalog.stop_operation @OperationId",
                    new { OperationId = PackageOperationId });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error stopping package operation id {operationId}", Configuration.ExecutionId, Step, PackageOperationId);
            }
        }
    }
}
