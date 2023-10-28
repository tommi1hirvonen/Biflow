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
    private readonly SqlStepExecution _step = step;

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogInformation("{ExecutionId} {Step} Starting SQL execution", _step.ExecutionId, _step);
            using var connection = new SqlConnection(_step.Connection.ConnectionString);
            connection.InfoMessage += Connection_InfoMessage;

            var parameters = _step.StepExecutionParameters
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue);
            var dynamicParams = new DynamicParameters(parameters);

            // command timeout = 0 => wait indefinitely
            var command = new CommandDefinition(
                _step.SqlStatement,
                commandTimeout: Convert.ToInt32(_step.TimeoutMinutes * 60),
                parameters: dynamicParams,
                cancellationToken: cancellationToken);

            // Check whether the query result should be captured to a job parameter.
            if (_step.ResultCaptureJobParameterId is not null)
            {
                var result = await connection.ExecuteScalarAsync(command);

                // Update the capture value.
                using var context = _dbContextFactory.CreateDbContext();
                context.Attach(_step);
                _step.ResultCaptureJobParameterValue = result;
                await context.SaveChangesAsync();

                // Update the job execution parameter with the result value for following steps to use.
                var param = _step.Execution.ExecutionParameters.FirstOrDefault(p => p.ParameterId == _step.ResultCaptureJobParameterId);
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
            var errors = ex.Errors.Cast<SqlError>();
            foreach (var error in errors)
            {
                var message = $"Line: {error.LineNumber}\nMessage: {error.Message}";
                AddError(ex, message);
            }

            // Return Cancel if the SqlCommand failed due to cancel being requested.
            // ExecuteNonQueryAsync() throws SqlException in case cancel was requested.
            if (cancellationToken.IsCancellationRequested)
            {
                AddWarning(ex);
                return Result.Cancel;
            }

            _logger.LogWarning(ex, "{ExecutionId} {Step} SQL execution failed", _step.ExecutionId, _step);

            return Result.Failure;
        }

        return Result.Success;
    }

    private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e) => AddOutput(e.Message);
}
