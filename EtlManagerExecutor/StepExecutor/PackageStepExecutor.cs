using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class PackageStepExecutor : StepExecutorBase
    {
        private PackageStepExecution Step { get; init; }
        private long PackageOperationId { get; set; }
        private bool Completed { get; set; }
        private bool Success { get; set; }

        private const int MaxRefreshRetries = 3;

        public PackageStepExecutor(ExecutionConfiguration configuration, PackageStepExecution step) : base(configuration)
        {
            Step = step;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Step.ExecuteAsLogin = string.IsNullOrEmpty(Step.ExecuteAsLogin) ? null : Step.ExecuteAsLogin;

            string connectionString;
            try
            {
                using var context = Configuration.DbContextFactory.CreateDbContext();
                var connection = await context.Connections
                    .Where(c => c.ConnectionId == Step.ConnectionId)
                    .FirstOrDefaultAsync(CancellationToken.None);
                connectionString = connection?.ConnectionString ?? throw new ArgumentNullException(nameof(connectionString), "Connection string was null");
                connection.ExecutePackagesAsLogin = string.IsNullOrEmpty(connection.ExecutePackagesAsLogin) ? null : connection.ExecutePackagesAsLogin;
                Step.ExecuteAsLogin ??= connection.ExecutePackagesAsLogin;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error getting connection string for package execution", Configuration.ExecutionId, Step);
                return new ExecutionResult.Failure($"Error getting connection string for connection id {Step.ConnectionId}: {ex.Message}");
            }

            // Start the package execution and capture the SSISDB operation id.
            DateTime startTime;
            try
            {
                Log.Information("{ExecutionId} {Step} Starting package execution", Configuration.ExecutionId, Step);

                await StartExecutionAsync(connectionString);
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
                using var context = Configuration.DbContextFactory.CreateDbContext();
                var attempt = Step.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == RetryAttemptCounter);
                if (attempt is not null && attempt is PackageStepExecutionAttempt package)
                {
                    package.PackageOperationId = PackageOperationId;
                    context.Attach(package);
                    context.Entry(package).Property(e => e.PackageOperationId).IsModified = true;
                    await context.SaveChangesAsync(CancellationToken.None);
                }
                else
                {
                    throw new InvalidOperationException("Could not find step execution attempt to update package operation id");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating target package operation id (" + PackageOperationId + ")", Configuration.ExecutionId, Step);
            }

            // Monitor the package's execution.
            try
            {
                await TryRefreshStatusAsync(connectionString, cancellationToken);
                while (!Completed)
                {
                    // Check for possible timeout.
                    if (Step.TimeoutMinutes > 0 && (DateTime.Now - startTime).TotalMinutes > Step.TimeoutMinutes)
                    {
                        await CancelAsync(connectionString);
                        Log.Warning("{ExecutionId} {Step} Step execution timed out", Configuration.ExecutionId, Step);
                        return new ExecutionResult.Failure("Step execution timed out");
                    }

                    await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
                    await TryRefreshStatusAsync(connectionString, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                await CancelAsync(connectionString);
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
                    List<string?> errors = await GetErrorMessagesAsync(connectionString);
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

        private async Task StartExecutionAsync(string connectionString)
        {
            var commandBuilder = new StringBuilder();
            
            commandBuilder.Append("USE SSISDB\n");
            
            if (Step.ExecuteAsLogin is not null)
                commandBuilder.Append("EXECUTE AS LOGIN = @ExecuteAsLogin\n");
            
            commandBuilder.Append(
                @"DECLARE @execution_id BIGINT

                EXEC [SSISDB].[catalog].[create_execution]
                    @package_name = @PackageName,
                    @execution_id = @execution_id OUTPUT,
                    @folder_name = @PackageFolderName,
                    @project_name = @PackageProjectName,
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

            foreach (var parameter in Step.StepExecutionParameters)
            {
                var objectType = parameter.ParameterLevel == "Project" ? "20" : "30"; // 20 => project parameter; 30 => package parameter
                // Same parameter name can be used for project and package parameter.
                // Use level in addition to name to uniquely identify each parameter.
                commandBuilder.Append(
                    @"EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                        @execution_id,
                        @object_type = " + objectType + @",
                        @parameter_name = @ParameterName" + parameter.ParameterName + parameter.ParameterLevel + @",
                        @parameter_value = @ParameterValue" + parameter.ParameterName + parameter.ParameterLevel + "\n"
                    );
            }

            commandBuilder.Append(
                @"EXEC [SSISDB].[catalog].[start_execution] @execution_id

                SELECT @execution_id"
                );

            string commandString = commandBuilder.ToString();
            var dynamicParams = new DynamicParameters();
            dynamicParams.AddDynamicParams(new { Step.PackageFolderName, Step.PackageProjectName, Step.PackageName, Step.ExecuteIn32BitMode });
            
            if (Step.ExecuteAsLogin is not null)
                dynamicParams.Add("ExecuteAsLogin", Step.ExecuteAsLogin);
           
            foreach (var param in Step.StepExecutionParameters)
            {
                dynamicParams.Add($"ParameterName{param.ParameterName}{param.ParameterLevel}", param.ParameterName);
                dynamicParams.Add($"ParameterValue{param.ParameterName}{param.ParameterLevel}", param.ParameterValue);
            }

            using var sqlConnection = new SqlConnection(connectionString);
            PackageOperationId = await sqlConnection.ExecuteScalarAsync<long>(commandString, dynamicParams);
        }

        private async Task TryRefreshStatusAsync(string connectionString, CancellationToken cancellationToken)
        {
            int refreshRetries = 0;
            // Try to refresh the operation status until the maximum number of attempts is reached.
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    using var sqlConnection = new SqlConnection(connectionString);
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

        private async Task<List<string?>> GetErrorMessagesAsync(string connectionString)
        {
            using var sqlConnection = new SqlConnection(connectionString);
            var messages = await sqlConnection.QueryAsync<string?>(
                @"SELECT message
                FROM SSISDB.catalog.operation_messages
                WHERE message_type = 120 AND operation_id = @OperationId", // message_type = 120 => error message
                new { OperationId = PackageOperationId });
            return messages.ToList();
        }

        private async Task CancelAsync(string connectionString)
        {
            Log.Information("{ExecutionId} {Step} Stopping package operation id {PackageOperationId}", Configuration.ExecutionId, Step, PackageOperationId);
            try
            {
                using var sqlConnection = new SqlConnection(connectionString);
                var commandBuilder = new StringBuilder();
                commandBuilder.Append("USE SSISDB\n");

                if (Step.ExecuteAsLogin is not null)
                    commandBuilder.Append("EXECUTE AS LOGIN = @ExecuteAsLogin\n");
                
                commandBuilder.Append("EXEC SSISDB.catalog.stop_operation @OperationId");
                
                var dynamicParams = new DynamicParameters();
                dynamicParams.AddDynamicParams(new { OperationId = PackageOperationId });
                
                if (Step.ExecuteAsLogin is not null)
                    dynamicParams.Add("ExecuteAsLogin", Step.ExecuteAsLogin);

                var command = commandBuilder.ToString();
                await sqlConnection.ExecuteAsync(command, dynamicParams);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error stopping package operation id {operationId}", Configuration.ExecutionId, Step, PackageOperationId);
            }
        }
    }
}
