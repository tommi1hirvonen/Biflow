using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class SqlStepExecutor : IStepExecutor
    {
        private StringBuilder InfoMessageBuilder { get; } = new StringBuilder();
        private SqlStepExecution Step { get; init; }

        public int RetryAttemptCounter { get; set; } = 0;

        public SqlStepExecutor(SqlStepExecution step)
        {
            Step = step;
        }

        public async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource.Token;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                Log.Information("{ExecutionId} {Step} Starting SQL execution", Step.ExecutionId, Step);
                using var connection = new SqlConnection(Step.Connection.ConnectionString);
                connection.InfoMessage += Connection_InfoMessage;
                // command timeout = 0 => wait indefinitely
                var command = new CommandDefinition(Step.SqlStatement, commandTimeout: Step.TimeoutMinutes * 60, cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
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
}
