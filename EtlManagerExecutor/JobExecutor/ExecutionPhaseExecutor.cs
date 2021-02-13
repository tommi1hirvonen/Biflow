using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class ExecutionPhaseExecutor : ExecutorBase
    {
        public ExecutionPhaseExecutor(ExecutionConfiguration executionConfiguration) : base(executionConfiguration) { }

        public override async Task RunAsync()
        {
            // Fetch all steps for this execution along with their execution phase.
            var allSteps = new List<KeyValuePair<int, Step>>();

            using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
            await sqlConnection.OpenAsync();
            var sqlCommand = new SqlCommand("SELECT DISTINCT StepId, StepName, ExecutionPhase FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", ExecutionConfig.ExecutionId);
            using (var reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    var stepId = reader["StepId"].ToString();
                    var stepName = reader["StepName"].ToString();
                    var executionPhase = (int)reader["ExecutionPhase"];
                    var step = new Step(stepId, stepName);
                    allSteps.Add(new(executionPhase, step));
                    CancellationTokenSources[stepId] = new();
                }
            }

            List<int> executionPhases = allSteps.Select(step => step.Key).Distinct().ToList();
            executionPhases.Sort();

            // Start listening for cancel commands.
            RegisterCancelListeners();

            // Start all steps in each execution phase in parallel.
            foreach (int executionPhase in executionPhases)
            {
                // Get a list of steps for this execution phase.
                List<Step> stepsToExecute = allSteps.Where(step => step.Key == executionPhase).Select(step => step.Value).ToList();
                var stepWorkers = new List<Task>();

                foreach (var step in stepsToExecute)
                {
                    stepWorkers.Add(StartNewStepWorkerAsync(step));
                }

                // All steps have been started. Wait until all step worker tasks have finished.
                await Task.WhenAll(stepWorkers);
            }
        }

    }
}
