using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    abstract class StepExecutionBase
    {
        protected ExecutionConfiguration Configuration { get; init; }
        protected Step Step { get; init; }
        public int RetryAttemptCounter { get; set; }

        public StepExecutionBase(ExecutionConfiguration configuration, Step step)
        {
            Configuration = configuration;
            Step = step;
        }

        public abstract Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken);

        protected async Task<Dictionary<string, object>> GetStepParameters()
        {
            var parameters = new Dictionary<string, object>();
            
            using var sqlConnection = new SqlConnection(Configuration.ConnectionString);
            var paramsCommand = new SqlCommand(
                @"SELECT [ParameterName], [ParameterValue]
                    FROM [etlmanager].[ExecutionParameter]
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                , sqlConnection);
            paramsCommand.Parameters.AddWithValue("@StepId", Step.StepId);
            paramsCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);

            await sqlConnection.OpenAsync();
            using var reader = await paramsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader["ParameterName"].ToString();
                var value = reader["ParameterValue"].ToString();
                parameters[name] = value;
            }

            return parameters;
        }
    }
}
