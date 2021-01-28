using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class SqlStepExecution : SqlStep, IExecutable
    {
        private ExecutionConfiguration ExecutionConfiguration { get; init; }
        private int TimeoutMinutes { get; init; }
        private StringBuilder InfoMessageBuilder { get; } = new StringBuilder();

        public SqlStepExecution(ExecutionConfiguration executionConfiguration, string stepId, string sqlStatement, string connectionString, int timeoutMinutes)
            : base(executionConfiguration, stepId, sqlStatement, connectionString)
        {
            ExecutionConfiguration = executionConfiguration;
            TimeoutMinutes = timeoutMinutes;
        }

        public async Task<ExecutionResult> ExecuteAsync()
        {
            try
            {
                Log.Information("{ExecutionId} {StepId} Starting SQL execution", ExecutionConfiguration.ExecutionId, StepId);
                using SqlConnection connection = new SqlConnection(ConnectionString);
                connection.InfoMessage += Connection_InfoMessage;
                await connection.OpenIfClosedAsync();
                SqlCommand sqlCommand = new SqlCommand(SqlStatement, connection)
                {
                    CommandTimeout = TimeoutMinutes * 60 // CommandTimeout = 0 => wait indefinitely
                };
                await sqlCommand.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
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
