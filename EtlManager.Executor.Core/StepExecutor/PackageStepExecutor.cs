using Dapper;
using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EtlManager.Executor.Core.StepExecutor;

internal class PackageStepExecutor : StepExecutorBase
{
    private readonly ILogger<PackageStepExecutor> _logger;
    private readonly IExecutionConfiguration _executionConfiguration;
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

    private PackageStepExecution Step { get; init; }

    private const int MaxRefreshRetries = 3;

    public PackageStepExecutor(
        ILogger<PackageStepExecutor> logger,
        IExecutionConfiguration executionConfiguration,
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        PackageStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _executionConfiguration = executionConfiguration;
        _dbContextFactory = dbContextFactory;
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        Step.ExecuteAsLogin = string.IsNullOrEmpty(Step.ExecuteAsLogin) ? null : Step.ExecuteAsLogin;
        Step.Connection.ExecutePackagesAsLogin = string.IsNullOrEmpty(Step.Connection.ExecutePackagesAsLogin) ? null : Step.Connection.ExecutePackagesAsLogin;
        Step.ExecuteAsLogin ??= Step.Connection.ExecutePackagesAsLogin;

        // Start the package execution and capture the SSISDB operation id.
        long packageOperationId;
        try
        {
            _logger.LogInformation("{ExecutionId} {Step} Starting package execution", Step.ExecutionId, Step);
            packageOperationId = await StartExecutionAsync(Step.Connection.ConnectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error executing package", Step.ExecutionId, Step);
            return Result.Failure("Error executing package: " + ex.Message);
        }

        using var timeoutCts = Step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
            : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Update the SSISDB operation id for the target package execution.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = Step.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == RetryAttemptCounter);
            if (attempt is not null && attempt is PackageStepExecutionAttempt package)
            {
                package.PackageOperationId = packageOperationId;
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
            _logger.LogError(ex, "{ExecutionId} {Step} Error updating target package operation id (" + packageOperationId + ")", Step.ExecutionId, Step);
        }

        // Monitor the package's execution.
        bool completed = false, success = false;
        try
        {
            while (!completed)
            {
                (completed, success) = await TryRefreshStatusAsync(Step.Connection.ConnectionString, packageOperationId, linkedCts.Token);
                if (!completed)
                    await Task.Delay(_executionConfiguration.PollingIntervalMs, linkedCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            await CancelAsync(Step.Connection.ConnectionString, packageOperationId);
            if (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("{ExecutionId} {Step} Step execution timed out", Step.ExecutionId, Step);
                return Result.Failure("Step execution timed out"); // Report failure => allow possible retries
            }
            throw; // Step was canceled => pass the exception => no retries
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error monitoring package execution status", Step.ExecutionId, Step);
            return Result.Failure("Error monitoring package execution status: " + ex.Message);
        }

        // The package has completed. If the package failed, retrieve error messages.
        if (!success)
        {
            try
            {
                List<string?> errors = await GetErrorMessagesAsync(Step.Connection.ConnectionString, packageOperationId);
                return Result.Failure(string.Join("\n\n", errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error getting package error messages", Step.ExecutionId, Step);
                return Result.Failure("Error getting package error messages: " + ex.Message);
            }
        }

        return Result.Success();
    }

    private async Task<long> StartExecutionAsync(string connectionString)
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

        foreach (var parameter in Step.StepExecutionParameters.Cast<PackageStepExecutionParameter>())
        {
            var objectType = parameter.ParameterLevel == ParameterLevel.Project ? "20" : "30"; // 20 => project parameter; 30 => package parameter
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

        foreach (var param in Step.StepExecutionParameters.Cast<PackageStepExecutionParameter>())
        {
            dynamicParams.Add($"ParameterName{param.ParameterName}{param.ParameterLevel}", param.ParameterName);
            dynamicParams.Add($"ParameterValue{param.ParameterName}{param.ParameterLevel}", param.ParameterValue);
        }

        using var sqlConnection = new SqlConnection(connectionString);
        var packageOperationId = await sqlConnection.ExecuteScalarAsync<long>(commandString, dynamicParams);
        return packageOperationId;
    }

    private async Task<(bool Completed, bool Success)> TryRefreshStatusAsync(string connectionString, long packageOperationId, CancellationToken cancellationToken)
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
                    new { OperationId = packageOperationId });
                // created (1), running (2), canceled (3), failed (4), pending (5), ended unexpectedly (6), succeeded (7), stopping (8), completed (9)
                if (status == 3 || status == 4 || status == 6 || status == 9)
                {
                    return (true, false); // failed
                }
                else if (status == 7)
                {
                    return (true, true); // success
                }

                return (false, false); // running
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing package operation status for operation id {operationId}", packageOperationId);
                refreshRetries++;
                await Task.Delay(_executionConfiguration.PollingIntervalMs, cancellationToken);
            }
        }
        // The maximum number of attempts was reached. Notify caller with exception.
        throw new TimeoutException("The maximum number of package operation status refresh attempts was reached.");
    }

    private static async Task<List<string?>> GetErrorMessagesAsync(string connectionString, long packageOperationId)
    {
        using var sqlConnection = new SqlConnection(connectionString);
        var messages = await sqlConnection.QueryAsync<string?>(
            @"SELECT message
                FROM SSISDB.catalog.operation_messages
                WHERE message_type = 120 AND operation_id = @OperationId", // message_type = 120 => error message
            new { OperationId = packageOperationId });
        return messages.ToList();
    }

    private async Task CancelAsync(string connectionString, long packageOperationId)
    {
        _logger.LogInformation("{ExecutionId} {Step} Stopping package operation id {PackageOperationId}", Step.ExecutionId, Step, packageOperationId);
        try
        {
            using var sqlConnection = new SqlConnection(connectionString);
            var commandBuilder = new StringBuilder();
            commandBuilder.Append("USE SSISDB\n");

            if (Step.ExecuteAsLogin is not null)
                commandBuilder.Append("EXECUTE AS LOGIN = @ExecuteAsLogin\n");

            commandBuilder.Append("EXEC SSISDB.catalog.stop_operation @OperationId");

            var dynamicParams = new DynamicParameters();
            dynamicParams.AddDynamicParams(new { OperationId = packageOperationId });

            if (Step.ExecuteAsLogin is not null)
                dynamicParams.Add("ExecuteAsLogin", Step.ExecuteAsLogin);

            var command = commandBuilder.ToString();
            await sqlConnection.ExecuteAsync(command, dynamicParams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error stopping package operation id {operationId}", Step.ExecutionId, Step, packageOperationId);
        }
    }
}
