using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class SqlStepExecutor(
    ILogger<SqlStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    SqlStepExecution step) : StepExecutorBase(logger, dbContextFactory, step)
{
    private readonly ILogger<SqlStepExecutor> _logger = logger;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    private SqlStepExecution Step { get; } = step;

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogInformation("{ExecutionId} {Step} Starting SQL execution", Step.ExecutionId, Step);
            using var connection = new SqlConnection(Step.Connection.ConnectionString);
            connection.InfoMessage += Connection_InfoMessage;

            var parameters = Step.StepExecutionParameters
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue);
            var dynamicParams = new DynamicParameters(parameters);

            // command timeout = 0 => wait indefinitely
            var command = new CommandDefinition(
                Step.SqlStatement,
                commandTimeout: Convert.ToInt32(Step.TimeoutMinutes * 60),
                parameters: dynamicParams,
                cancellationToken: cancellationToken);

            // Check whether the query result should be captured to a job parameter.
            if (Step.ResultCaptureJobParameterId is not null)
            {
                var result = await connection.ExecuteScalarAsync(command);

                // Update the capture value.
                using var context = _dbContextFactory.CreateDbContext();
                context.Attach(Step);
                Step.ResultCaptureJobParameterValue = result;
                await context.SaveChangesAsync();

                // Update the job execution parameter with the result value for following steps to use.
                var param = Step.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == Step.ResultCaptureJobParameterId);
                if (param is not null)
                {
                    param.ParameterValue = result;
                }
            }
            else
            {
                await connection.ExecuteAsync(command);
            }
        }
        catch (SqlException ex)
        {
            // Return Cancel if the SqlCommand failed due to cancel being requested.
            // ExecuteNonQueryAsync() throws SqlException in case cancel was requested.
            if (cancellationToken.IsCancellationRequested)
            {
                return new Cancel(ex);
            }

            _logger.LogWarning(ex, "{ExecutionId} {Step} SQL execution failed", Step.ExecutionId, Step);
            var errors = ex.Errors.Cast<SqlError>();
            var errorMessage = string.Join("\n\n", errors.Select(error => "Line: " + error.LineNumber + "\nMessage: " + error.Message));
            return new Failure(errorMessage);
        }

        return new Success();
    }

    private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e) => AddOutput(e.Message);
}
