using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class SqlStepExecutor(
    ILogger<SqlStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<SqlStepExecution, SqlStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<SqlStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    protected override Task<Result> ExecuteAsync(
        SqlStepExecution step,
        SqlStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var connection = step.GetConnection();
        ArgumentNullException.ThrowIfNull(connection);

        switch (connection)
        {
            case MsSqlConnection mssql:
                return ExecuteMsSqlAsync(step, attempt, mssql, cancellationToken);
            case SnowflakeConnection sf:
                return ExecuteSnowflakeAsync(step, attempt, sf, cancellationToken);
            default:
                _logger.LogError("Unsupported connection type: {connectionType}. Connection must be of type {msSqlType} or {snowFlakeType}.",
                    connection.GetType().Name, nameof(MsSqlConnection), nameof(SnowflakeConnection));
                attempt.AddError($"Unsupported connection type: {connection.GetType().Name}. Connection must be of type {nameof(MsSqlConnection)} or {nameof(SnowflakeConnection)}.");
                return Task.FromResult(Result.Failure);
        }
    }

    private async Task<Result> ExecuteMsSqlAsync(
        SqlStepExecution step,
        SqlStepExecutionAttempt attempt,
        MsSqlConnection msSqlConnection,
        CancellationToken cancellationToken)
    {

        if (msSqlConnection.Credential is not null && !OperatingSystem.IsWindows())
        {
            attempt.AddWarning("Connection has impersonation enabled but the OS platform does not support it. Impersonation will be skipped.");
        }

        try
        {
            _logger.LogInformation("{ExecutionId} {Step} Starting SQL execution with MSSQL connector", step.ExecutionId, step);
            await using var connection = new SqlConnection(msSqlConnection.ConnectionString);
            connection.InfoMessage += (_, eventArgs) => attempt.AddOutput(eventArgs.Message);

            var parameters = step.StepExecutionParameters
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue.Value);
            var dynamicParams = new DynamicParameters(parameters);

            // command timeout = 0 => wait indefinitely
            var command = new CommandDefinition(
                step.SqlStatement,
                commandTimeout: Convert.ToInt32(step.TimeoutMinutes * 60),
                parameters: dynamicParams,
                cancellationToken: cancellationToken);

            // Check whether the query result should be captured to a job parameter.
            if (step.ResultCaptureJobParameterId is not null)
            {
                var result = await msSqlConnection.RunImpersonatedOrAsCurrentUserAsync(
                    () => connection.ExecuteScalarAsync(command));

                // Update the capture value.
                await using var context = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
                step.ResultCaptureJobParameterValue = new(result);

                // Update the job execution parameter with the result value for following steps to use.
                var param = step.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == step.ResultCaptureJobParameterId);
                if (param is not null)
                {
                    param.ParameterValue = new(result);
                    await context.Set<ExecutionParameter>()
                        .Where(x => x.ExecutionId == param.ExecutionId && x.ParameterId == param.ParameterId)
                        .ExecuteUpdateAsync(x => x
                            .SetProperty(p => p.ParameterValue, param.ParameterValue), CancellationToken.None);
                }

                await context.Set<SqlStepExecution>()
                    .Where(x => x.ExecutionId == step.ExecutionId && x.StepId == step.StepId)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(p => p.ResultCaptureJobParameterValue, step.ResultCaptureJobParameterValue), CancellationToken.None);
            }
            else
            {
                await msSqlConnection.RunImpersonatedOrAsCurrentUserAsync(
                    () => connection.ExecuteAsync(command));
            }
        }
        catch (SqlException ex)
        {
            var errors = ex.Errors.Cast<SqlError>();
            foreach (var error in errors)
            {
                var message = $"Line: {error.LineNumber}\nMessage: {error.Message}";
                attempt.AddError(ex, message);
            }

            // Return Cancel if the SqlCommand failed due to cancel being requested.
            // ExecuteNonQueryAsync() throws SqlException in case cancel was requested.
            if (cancellationToken.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }

            _logger.LogWarning(ex, "{ExecutionId} {Step} SQL execution failed", step.ExecutionId, step);

            return Result.Failure;
        }

        return Result.Success;
    }

    private async Task<Result> ExecuteSnowflakeAsync(
        SqlStepExecution step,
        SqlStepExecutionAttempt attempt,
        SnowflakeConnection sfConnection,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("{ExecutionId} {Step} Starting SQL execution with Snowflake connector", step.ExecutionId, step);
            await using var connection = new SnowflakeDbConnection(sfConnection.ConnectionString);

            var parameters = step.StepExecutionParameters
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue.Value);
            var dynamicParams = new DynamicParameters(parameters);

            // command timeout = 0 => wait indefinitely
            var command = new CommandDefinition(
                step.SqlStatement,
                commandTimeout: Convert.ToInt32(step.TimeoutMinutes * 60),
                parameters: dynamicParams,
                cancellationToken: cancellationToken);

            // Check whether the query result should be captured to a job parameter.
            if (step.ResultCaptureJobParameterId is not null)
            {
                var result = await connection.ExecuteScalarAsync(command);

                // Update the capture value.
                await using var context = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
                step.ResultCaptureJobParameterValue = new(result);

                // Update the job execution parameter with the result value for following steps to use.
                var param = step.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == step.ResultCaptureJobParameterId);
                if (param is not null)
                {
                    param.ParameterValue = new(result);
                    await context.Set<ExecutionParameter>()
                        .Where(x => x.ExecutionId == param.ExecutionId && x.ParameterId == param.ParameterId)
                        .ExecuteUpdateAsync(x => x
                            .SetProperty(p => p.ParameterValue, param.ParameterValue), CancellationToken.None);
                }

                await context.Set<SqlStepExecution>()
                    .Where(x => x.ExecutionId == step.ExecutionId && x.StepId == step.StepId)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(p => p.ResultCaptureJobParameterValue, step.ResultCaptureJobParameterValue), CancellationToken.None);
            }
            else
            {
                await connection.ExecuteAsync(command);
            }
        }
        catch (SnowflakeDbException ex)
        {
            attempt.AddError(ex, ex.Message);

            // Return Cancel if the SqlCommand failed due to cancel being requested.
            // ExecuteNonQueryAsync() throws SqlException in case cancel was requested.
            if (cancellationToken.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }

            _logger.LogWarning(ex, "{ExecutionId} {Step} SQL execution failed", step.ExecutionId, step);

            return Result.Failure;
        }

        return Result.Success;
    }
}
