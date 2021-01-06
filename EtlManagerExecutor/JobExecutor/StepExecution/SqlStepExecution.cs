using Serilog;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class SqlStepExecution : IStepExecution
    {
        private readonly ExecutionConfiguration executionConfig;
        private readonly SqlStepConfiguration sqlStep;

        private StringBuilder InfoMessageBuilder { get; } = new StringBuilder();

        public SqlStepExecution(ExecutionConfiguration executionConfiguration, SqlStepConfiguration sqlStepConfiguration)
        {
            executionConfig = executionConfiguration;
            sqlStep = sqlStepConfiguration;
        }

        public async Task<ExecutionResult> RunAsync()
        {
            try
            {
                Log.Information("{ExecutionId} {StepId} Starting SQL execution", executionConfig.ExecutionId, sqlStep.StepId);
                using SqlConnection connection = new SqlConnection(sqlStep.ConnectionString);
                connection.InfoMessage += Connection_InfoMessage;
                connection.OpenIfClosed();
                SqlCommand sqlCommand = new SqlCommand(sqlStep.SqlStatement, connection) { CommandTimeout = 0 }; // CommandTimeout = 0 => wait indefinitely
                await sqlCommand.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                Log.Warning(ex, "{ExecutionId} {StepId} SQL execution failed", executionConfig.ExecutionId, sqlStep.StepId);
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
