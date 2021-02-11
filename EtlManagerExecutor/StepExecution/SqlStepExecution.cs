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
    class SqlStepExecution : IExecutable
    {
        private ExecutionConfiguration ExecutionConfiguration { get; init; }
        private string StepId { get; init; }
        private string SqlStatement { get; init; }
        private string ConnectionString { get; init; }
        private int TimeoutMinutes { get; init; }
        private StringBuilder InfoMessageBuilder { get; } = new StringBuilder();
        public int RetryAttemptCounter { get; set; }

        public SqlStepExecution(ExecutionConfiguration executionConfiguration, string stepId, string sqlStatement, string connectionString, int timeoutMinutes)
        {
            ExecutionConfiguration = executionConfiguration;
            StepId = stepId;
            SqlStatement = sqlStatement;
            ConnectionString = connectionString;
            TimeoutMinutes = timeoutMinutes;
        }

        public async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("{ExecutionId} {StepId} Starting SQL execution", ExecutionConfiguration.ExecutionId, StepId);
                using var connection = new SqlConnection(ConnectionString);
                connection.InfoMessage += Connection_InfoMessage;
                await connection.OpenAsync();
                SqlCommand sqlCommand = new SqlCommand(SqlStatement, connection)
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

                Log.Warning(ex, "{ExecutionId} {StepId} SQL execution failed", ExecutionConfiguration.ExecutionId, StepId);
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
