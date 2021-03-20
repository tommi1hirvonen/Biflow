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

        protected async Task<HashSet<Parameter>> GetStepParameters()
        {
            var parameters = new HashSet<Parameter>();
            
            using var sqlConnection = new SqlConnection(Configuration.ConnectionString);
            using var paramsCommand = new SqlCommand(
                @"SELECT ParameterName, ParameterValue, ParameterLevel
                    FROM etlmanager.ExecutionParameter
                    WHERE ExecutionId = @ExecutionId AND StepId = @StepId"
                , sqlConnection);
            paramsCommand.Parameters.AddWithValue("@StepId", Step.StepId);
            paramsCommand.Parameters.AddWithValue("@ExecutionId", Configuration.ExecutionId);

            await sqlConnection.OpenAsync();
            using var reader = await paramsCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader["ParameterName"].ToString();
                object value = reader["ParameterValue"];
                var level = reader["ParameterLevel"].ToString();
                parameters.Add(new(name, value, level));
            }

            return parameters;
        }
    }
}
