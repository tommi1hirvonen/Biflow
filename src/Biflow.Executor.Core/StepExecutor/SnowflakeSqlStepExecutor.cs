using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;

namespace Biflow.Executor.Core.StepExecutor;

internal class SnowflakeSqlStepExecutor(
    IServiceProvider serviceProvider,
    SqlStepExecution step,
    SqlStepExecutionAttempt attempt,
    SnowflakeConnection snowflakeConnection) : IStepExecutor
{
    private readonly ILogger<SnowflakeSqlStepExecutor> _logger = serviceProvider
        .GetRequiredService<ILogger<SnowflakeSqlStepExecutor>>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = serviceProvider
        .GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
    
    public async Task<Result> ExecuteAsync(OrchestrationContext context, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogInformation("{ExecutionId} {Step} Starting SQL execution with Snowflake connector",
                step.ExecutionId, step);
            await using var connection = new SnowflakeDbConnection(snowflakeConnection.ConnectionString);

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
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync(CancellationToken.None);
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
