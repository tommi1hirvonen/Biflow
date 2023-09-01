using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Text;

namespace Biflow.Executor.Core.StepExecutor;

internal class PackageStepExecutor : StepExecutorBase
{
    private readonly ILogger<PackageStepExecutor> _logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly int _pollingIntervalMs;

    private PackageStepExecution Step { get; }

    private const int MaxRefreshRetries = 3;

    public PackageStepExecutor(
        ILogger<PackageStepExecutor> logger,
        IOptionsMonitor<ExecutionOptions> options,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        PackageStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _pollingIntervalMs = options.CurrentValue.PollingIntervalMs;
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
            return new Failure(ex, "Error starting package execution");
        }

        // Initialize timeout cancellation token source already here
        // so that we can start the countdown immediately after the package was started.
        using var timeoutCts = Step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
            : new CancellationTokenSource();

        // Update the SSISDB operation id for the target package execution.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = Step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
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
            _logger.LogError(ex, "{ExecutionId} {Step} Error updating target package operation id ({packageOperationId})", Step.ExecutionId, Step, packageOperationId);
            AddWarning(ex, $"Error updating target package operation id {packageOperationId}");
        }

        // Monitor the package's execution.
        bool completed = false, success = false;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            while (!completed)
            {
                (completed, success) = await GetStatusWithRetriesAsync(Step.Connection.ConnectionString, packageOperationId, linkedCts.Token);
                if (!completed)
                    await Task.Delay(_pollingIntervalMs, linkedCts.Token);
            }
        }
        catch (OperationCanceledException ex)
        {
            await CancelAsync(Step.Connection.ConnectionString, packageOperationId);
            return timeoutCts.IsCancellationRequested
                ? new Failure(ex, "Step execution timed out")
                : new Cancel(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error monitoring package execution status", Step.ExecutionId, Step);
            return new Failure(ex, "Error monitoring package execution status");
        }

        // The package has completed. If the package failed, retrieve error messages.
        if (!success)
        {
            try
            {
                var errors = await GetErrorMessagesAsync(Step.Connection.ConnectionString, packageOperationId);
                return new Failure(string.Join("\n\n", errors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error getting package error messages", Step.ExecutionId, Step);
                return new Failure(ex, "Error getting package error messages");
            }
        }

        return new Success();
    }

    private async Task<long> StartExecutionAsync(string connectionString)
    {
        var commandBuilder = new StringBuilder();

        commandBuilder.Append("USE SSISDB\n");

        if (Step.ExecuteAsLogin is not null)
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
                @parameter_value = 0

            """);

        foreach (var parameter in Step.StepExecutionParameters.Cast<PackageStepExecutionParameter>())
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
            EXEC [SSISDB].[catalog].[start_execution] @execution_id

            SELECT @execution_id
            """);

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

    private async Task<(bool Completed, bool Success)> GetStatusWithRetriesAsync(string connectionString, long packageOperationId, CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
            retryCount: MaxRefreshRetries,
            sleepDurationProvider: retryCount => TimeSpan.FromMilliseconds(_pollingIntervalMs),
            onRetry: (ex, waitDuration) =>
                _logger.LogWarning(ex, "Error getting package operation status for operation id {operationId}", packageOperationId));

        return await policy.ExecuteAsync(async (cancellationToken) =>
        {
            using var sqlConnection = new SqlConnection(connectionString);
            var command = new CommandDefinition("SELECT status from SSISDB.catalog.operations where operation_id = @OperationId",
                new { OperationId = packageOperationId },
                cancellationToken: cancellationToken);
            var status = await sqlConnection.ExecuteScalarAsync<int>(command);
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
        }, cancellationToken);
    }

    private static async Task<string?[]> GetErrorMessagesAsync(string connectionString, long packageOperationId)
    {
        using var sqlConnection = new SqlConnection(connectionString);
        var messages = await sqlConnection.QueryAsync<string?>("""
            SELECT message
            FROM SSISDB.catalog.operation_messages
            WHERE message_type = 120 AND operation_id = @OperationId
            """, // message_type = 120 => error message
            new { OperationId = packageOperationId });
        return messages.ToArray();
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
            AddWarning(ex, $"Error stopping package operation for id {packageOperationId}");
        }
    }
}
