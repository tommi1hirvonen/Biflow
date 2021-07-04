using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class SqlStepExecutionBuilder : IStepExecutionBuilder
    {
        public async Task<StepExecutionBase> CreateAsync(ExecutionConfiguration config, Step step, SqlConnection sqlConnection)
        {
            using var stepDetailsCmd = new SqlCommand(
                @"SELECT TOP 1
                    SqlStatement,
                    ConnectionString = etlmanager.GetConnectionStringDecrypted(ConnectionId, @EncryptionPassword),
                    TimeoutMinutes
                FROM etlmanager.Execution
                WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                , sqlConnection);
            stepDetailsCmd.Parameters.AddWithValue("@ExecutionId", config.ExecutionId);
            stepDetailsCmd.Parameters.AddWithValue("@StepId", step.StepId);
            stepDetailsCmd.Parameters.AddWithValue("@EncryptionPassword", config.EncryptionKey);
            using var reader = await stepDetailsCmd.ExecuteReaderAsync(CancellationToken.None);
            if (await reader.ReadAsync(CancellationToken.None))
            {
                var sqlStatement = reader["SqlStatement"].ToString()!;
                var connectionString = reader["ConnectionString"].ToString()!;
                var timeoutMinutes = (int)reader["TimeoutMinutes"];
                return new SqlStepExecution(config, step, sqlStatement, connectionString, timeoutMinutes);
            }
            else
            {
                throw new InvalidOperationException("Could not find step execution details");
            }
        }
    }

    class SqlStepExecution : StepExecutionBase
    {
        private string SqlStatement { get; init; }
        private string ConnectionString { get; init; }
        private int TimeoutMinutes { get; init; }
        private StringBuilder InfoMessageBuilder { get; } = new StringBuilder();

        public SqlStepExecution(ExecutionConfiguration executionConfiguration, Step step, string sqlStatement, string connectionString, int timeoutMinutes)
            : base(executionConfiguration, step)
        {
            SqlStatement = sqlStatement;
            ConnectionString = connectionString;
            TimeoutMinutes = timeoutMinutes;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("{ExecutionId} {Step} Starting SQL execution", Configuration.ExecutionId, Step);
                using var connection = new SqlConnection(ConnectionString);
                connection.InfoMessage += Connection_InfoMessage;
                await connection.OpenAsync(CancellationToken.None);
                using var sqlCommand = new SqlCommand(SqlStatement, connection)
                {
                    CommandTimeout = TimeoutMinutes * 60 // CommandTimeout = 0 => wait indefinitely
                };
                await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                // Throw OperationCanceledException if the SqlCommand failed due to cancel being requested.
                // ExecuteNonQueryAsync() throws SqlException in case cancel was requested.
                cancellationToken.ThrowIfCancellationRequested();

                Log.Warning(ex, "{ExecutionId} {Step} SQL execution failed", Configuration.ExecutionId, Step);
                var errors = ex.Errors.Cast<SqlError>();
                var errorMessage = string.Join("\n\n", errors.Select(error => "Line: " + error.LineNumber + "\nMessage: " + error.Message));
                return new ExecutionResult.Failure(errorMessage, InfoMessageBuilder.ToString());
            }

            return new ExecutionResult.Success(InfoMessageBuilder.ToString());
        }

        private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            InfoMessageBuilder.AppendLine(e.Message);
        }
    }
}
