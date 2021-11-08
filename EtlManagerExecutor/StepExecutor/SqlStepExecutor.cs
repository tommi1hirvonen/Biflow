using Dapper;
using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor;

class SqlStepExecutor : StepExecutorBase
{
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

    private StringBuilder InfoMessageBuilder { get; } = new StringBuilder();
    private SqlStepExecution Step { get; init; }

    public SqlStepExecutor(
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        SqlStepExecution step)
        : base(dbContextFactory, step)
    {
        _dbContextFactory = dbContextFactory;
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Log.Information("{ExecutionId} {Step} Starting SQL execution", Step.ExecutionId, Step);
            using var connection = new SqlConnection(Step.Connection.ConnectionString);
            connection.InfoMessage += Connection_InfoMessage;

            var parameters = Step.StepExecutionParameters
                .ToDictionary(key => key.ParameterName, value => value.ParameterValue);
            var dynamicParams = new DynamicParameters(parameters);

            // command timeout = 0 => wait indefinitely
            var command = new CommandDefinition(
                Step.SqlStatement,
                commandTimeout: Step.TimeoutMinutes * 60,
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
            // Throw OperationCanceledException if the SqlCommand failed due to cancel being requested.
            // ExecuteNonQueryAsync() throws SqlException in case cancel was requested.
            cancellationToken.ThrowIfCancellationRequested();

            Log.Warning(ex, "{ExecutionId} {Step} SQL execution failed", Step.ExecutionId, Step);
            var errors = ex.Errors.Cast<SqlError>();
            var errorMessage = string.Join("\n\n", errors.Select(error => "Line: " + error.LineNumber + "\nMessage: " + error.Message));
            return Result.Failure(errorMessage, GetInfoMessage());
        }

        return Result.Success(GetInfoMessage());
    }

    private string? GetInfoMessage()
    {
        var infoMessage = InfoMessageBuilder.ToString();
        infoMessage = string.IsNullOrEmpty(infoMessage) ? null : infoMessage;
        return infoMessage;
    }

    private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
    {
        InfoMessageBuilder.AppendLine(e.Message);
    }
}
