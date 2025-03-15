using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class PackageStepExecutor(
    ILogger<PackageStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<PackageStepExecution, PackageStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<PackageStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    protected override async Task<Result> ExecuteAsync(
        OrchestrationContext context,
        PackageStepExecution step,
        PackageStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var connection = step.GetConnection();
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.Credential is not null && !OperatingSystem.IsWindows())
        {
            attempt.AddWarning("Connection has impersonation enabled but the OS platform does not support it. Impersonation will be skipped.");
        }

        var executeAsLogin = (connection.ExecutePackagesAsLogin, step.ExecuteAsLogin) switch
        {
            ({ Length: > 0 }, _) => connection.ExecutePackagesAsLogin,
            (_, { Length: > 0}) => step.ExecuteAsLogin,
            _ => null
        };

        // Create the package execution and capture the SSISDB operation id.
        long packageOperationId;
        try
        {
            _logger.LogInformation("{ExecutionId} {Step} Starting package execution", step.ExecutionId, step);
            packageOperationId = await CreatePackageExecutionAsync(step, connection, executeAsLogin, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error creating package execution", step.ExecutionId, step);
            attempt.AddError(ex, "Error creating package execution");
            return Result.Failure;
        }

        // Initialize timeout cancellation token source already here
        // so that we can start the countdown right before the package is started.
        using var timeoutCts = step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(step.TimeoutMinutes))
            : new CancellationTokenSource();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Start the package execution asynchronously. The task is awaited later.
        var packageRunTask = RunPackageExecutionAsync(packageOperationId, connection, executeAsLogin, linkedCts.Token);

        // In the meantime, persist the package operation id.
        try
        {
            await UpdateOperationIdAsync(step, attempt, packageOperationId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error updating SSIS package operation id", step.ExecutionId, step);
            attempt.AddWarning(ex, "Error updating package operation id");
        }

        try
        {
            // Wait until the package execution finishes.
            // If the task is cancelled, the package operation is also automatically cancelled by SSISDB.
            await packageRunTask; 
        }
        catch (Exception ex) when (linkedCts.IsCancellationRequested)
        {
            if (timeoutCts.IsCancellationRequested)
            {
                attempt.AddError(ex, "Step execution timed out");
                return Result.Failure;
            }
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error executing package", step.ExecutionId, step);
            attempt.AddError(ex, "Error executing package");
        }

        // Package run task finished. Get the operation status from SSISDB.
        bool success;
        try
        {
            success = await GetPackageStatusAsync(connection, packageOperationId, cancellationToken);
        }
        catch (Exception ex) when (cancellationTokenSource.IsCancellationRequested)
        {
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error getting package execution status", step.ExecutionId, step);
            attempt.AddError(ex, "Error getting package execution status");
            return Result.Failure;
        }

        // The package has completed.
        if (success)
        {
            return Result.Success;
        }
        
        // If the package failed, retrieve error messages.
        try
        {
            var errors = await GetErrorMessagesAsync(connection, packageOperationId, cancellationToken);
            foreach (var error in errors)
            {
                if (error is not null)
                    attempt.AddError(error);
            }
            return Result.Failure;
        }
        catch (Exception ex) when (cancellationTokenSource.IsCancellationRequested)
        {
            attempt.AddWarning(ex);
            return Result.Cancel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error getting package error messages", step.ExecutionId, step);
            attempt.AddError(ex, "Error getting package error messages");
            return Result.Failure;
        }
    }

    private static async Task<long> CreatePackageExecutionAsync(
        PackageStepExecution step,
        MsSqlConnection connection,
        string? executeAsLogin,
        CancellationToken cancellationToken)
    {
        var commandBuilder = new StringBuilder();

        commandBuilder.Append("USE SSISDB\n");

        if (executeAsLogin is not null)
            commandBuilder.Append("EXECUTE AS LOGIN = @ExecuteAsLogin\n");

        commandBuilder.Append("""
            DECLARE @execution_id BIGINT

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
                @parameter_value = 1

            """);

        foreach (var parameter in step.StepExecutionParameters)
        {
            var objectType = parameter.ParameterLevel == ParameterLevel.Project ? 20 : 30;  // 20 => project parameter; 30 => package parameter
                                                                                            // Same parameter name can be used for project and package parameter.
                                                                                            // Use level in addition to name to uniquely identify each parameter.
            commandBuilder.Append($"""
                EXEC [SSISDB].[catalog].[set_execution_parameter_value]
                    @execution_id,
                    @object_type = {objectType},
                    @parameter_name = @ParameterName{parameter.ParameterName}{parameter.ParameterLevel},
                    @parameter_value = @ParameterValue{parameter.ParameterName}{parameter.ParameterLevel}

                """);
        }

        commandBuilder.Append("""
            SELECT @execution_id

            """);

        var commandString = commandBuilder.ToString();
        var dynamicParams = new DynamicParameters();
        dynamicParams.AddDynamicParams(new { step.PackageFolderName, step.PackageProjectName, step.PackageName, step.ExecuteIn32BitMode });

        if (executeAsLogin is not null)
            dynamicParams.Add("ExecuteAsLogin", executeAsLogin);

        foreach (var param in step.StepExecutionParameters)
        {
            dynamicParams.Add($"ParameterName{param.ParameterName}{param.ParameterLevel}", param.ParameterName);
            dynamicParams.Add($"ParameterValue{param.ParameterName}{param.ParameterLevel}", param.ParameterValue.Value);
        }

        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var cmd = new CommandDefinition(commandString, dynamicParams, cancellationToken: cancellationToken);
        var packageOperationId = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.ExecuteScalarAsync<long>(cmd));
        return packageOperationId;
    }

    private static async Task RunPackageExecutionAsync(
        long packageOperationId,
        MsSqlConnection connection,
        string? executeAsLogin,
        CancellationToken cancellationToken)
    {
        var commandBuilder = new StringBuilder();

        commandBuilder.Append("USE SSISDB\n");

        if (executeAsLogin is not null)
            commandBuilder.Append("EXECUTE AS LOGIN = @ExecuteAsLogin\n");

        commandBuilder.Append("EXEC [SSISDB].[catalog].[start_execution] @packageOperationId");

        var commandString = commandBuilder.ToString();
        var dynamicParams = new DynamicParameters();
        dynamicParams.AddDynamicParams(new { packageOperationId });

        if (executeAsLogin is not null)
            dynamicParams.Add("ExecuteAsLogin", executeAsLogin);

        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var cmd = new CommandDefinition(commandString, dynamicParams, commandTimeout: 0, cancellationToken: cancellationToken);
        await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.ExecuteAsync(cmd));
    }

    private static async Task<bool> GetPackageStatusAsync(
        MsSqlConnection connection,
        long packageOperationId,
        CancellationToken cancellationToken)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var command = new CommandDefinition("""
            SELECT status
            FROM SSISDB.catalog.operations
            WHERE operation_id = @OperationId
            """,
            new { OperationId = packageOperationId },
            cancellationToken: cancellationToken);
        var status = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.ExecuteScalarAsync<int>(command));
        // created (1), running (2), canceled (3), failed (4), pending (5), ended unexpectedly (6), succeeded (7), stopping (8), completed (9)
        return status == 7;
    }

    private static async Task<string?[]> GetErrorMessagesAsync(
        MsSqlConnection connection,
        long packageOperationId,
        CancellationToken cancellationToken)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var cmd = new CommandDefinition("""
            SELECT message
            FROM SSISDB.catalog.operation_messages
            WHERE message_type = 120 AND operation_id = @OperationId
            """, // message_type = 120 => error message
            new { OperationId = packageOperationId },
            cancellationToken: cancellationToken);
        var messages = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<string?>(cmd));
        return messages.ToArray();
    }

    private async Task UpdateOperationIdAsync(
        PackageStepExecution step,
        PackageStepExecutionAttempt attempt,
        long packageOperationId,
        CancellationToken cancellationToken)
    {
        // Update the SSISDB operation id for the target package execution.
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            attempt.PackageOperationId = packageOperationId;
            await context.Set<PackageStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId && x.StepId == attempt.StepId && x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.PackageOperationId, attempt.PackageOperationId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error updating target package operation id ({packageOperationId})", step.ExecutionId, step, packageOperationId);
            attempt.AddWarning(ex, $"Error updating target package operation id {packageOperationId}");
        }
    }
}
