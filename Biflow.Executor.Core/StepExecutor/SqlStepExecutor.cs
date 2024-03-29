using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class SqlStepExecutor(
    ILogger<SqlStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<SqlStepExecution, SqlStepExecutionAttempt>(logger, dbContextFactory)
{
    private readonly ILogger<SqlStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    protected override async Task<Result> ExecuteAsync(
        SqlStepExecution step,
        SqlStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var connectionInfo = step.GetConnection();
        ArgumentNullException.ThrowIfNull(connectionInfo);

        try
        {
            _logger.LogInformation("{ExecutionId} {Step} Starting SQL execution", step.ExecutionId, step);
            using var connection = new SqlConnection(connectionInfo.ConnectionString);
            connection.InfoMessage += (s, e) => attempt.AddOutput(e.Message);

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
                using var context = _dbContextFactory.CreateDbContext();
                step.ResultCaptureJobParameterValue.Value = result;
                context.Attach(step).Property(p => p.ResultCaptureJobParameterValue).IsModified = true;

                // Update the job execution parameter with the result value for following steps to use.
                var param = step.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == step.ResultCaptureJobParameterId);
                if (param is not null)
                {
                    param.ParameterValue.Value = result;
                    context.Attach(param).Property(p => p.ParameterValue).IsModified = true;
                }

                await context.SaveChangesAsync();
            }
            else
            {
                await connection.ExecuteAsync(command);
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
}
