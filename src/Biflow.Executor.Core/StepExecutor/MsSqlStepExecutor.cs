using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class MsSqlStepExecutor(
    ILogger<MsSqlStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    SqlStepExecution step,
    SqlStepExecutionAttempt attempt,
    MsSqlConnection msSqlConnection) : IStepExecutor
{
    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        if (msSqlConnection.Credential is not null && !OperatingSystem.IsWindows())
        {
            attempt.AddWarning("Connection has impersonation enabled but the OS platform does not support it. Impersonation will be skipped.");
        }

        try
        {
            logger.LogInformation("{ExecutionId} {Step} Starting SQL execution with MSSQL connector", step.ExecutionId, step);
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
                await using var dbContext = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
                step.ResultCaptureJobParameterValue = new ParameterValue(result);

                // Update the job execution parameter with the result value for following steps to use.
                var param = step.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == step.ResultCaptureJobParameterId);
                if (param is not null)
                {
                    param.ParameterValue = new ParameterValue(result);
                    await dbContext.Set<ExecutionParameter>()
                        .Where(x => x.ExecutionId == param.ExecutionId && x.ParameterId == param.ParameterId)
                        .ExecuteUpdateAsync(x => x
                            .SetProperty(p => p.ParameterValue, param.ParameterValue), CancellationToken.None);
                }

                await dbContext.Set<SqlStepExecution>()
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

            logger.LogWarning(ex, "{ExecutionId} {Step} SQL execution failed", step.ExecutionId, step);

            return Result.Failure;
        }

        return Result.Success;
    }
}
