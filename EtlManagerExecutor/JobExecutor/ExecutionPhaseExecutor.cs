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
    class ExecutionPhaseExecutor : ExecutorBase
    {
        public ExecutionPhaseExecutor(ExecutionConfiguration executionConfiguration) : base(executionConfiguration) { }

        public override async Task RunAsync()
        {
            var allSteps = new List<KeyValuePair<int, string>>();

            using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
            await sqlConnection.OpenAsync();
            SqlCommand sqlCommand = new SqlCommand("SELECT DISTINCT StepId, ExecutionPhase FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
            sqlCommand.Parameters.AddWithValue("@ExecutionId", ExecutionConfig.ExecutionId);
            using (var reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    var step = new KeyValuePair<int, string>((int)reader["ExecutionPhase"], reader["StepId"].ToString());
                    allSteps.Add(step);
                }
            }

            List<int> executionPhases = allSteps.Select(step => step.Key).Distinct().ToList();
            executionPhases.Sort();

            foreach (int executionPhase in executionPhases)
            {
                List<string> stepsToExecute = allSteps.Where(step => step.Key == executionPhase).Select(step => step.Value).ToList();
                var stepWorkers = new List<Task>();

                foreach (string stepId in stepsToExecute)
                {
                    stepWorkers.Add(StartNewStepWorkerAsync(stepId));
                }

                // All steps have been started. Wait until all step worker tasks have finished.
                await Task.WhenAll(stepWorkers);
            }
        }

        private async Task StartNewStepWorkerAsync(string stepId)
        {
            // Wait until the semaphore can be entered and the step can be started.
            await Semaphore.WaitAsync();
            // Create a new step worker and start it asynchronously.
            var task = new StepWorker(ExecutionConfig, stepId).ExecuteStepAsync();
            Log.Information("{ExecutionId} {stepId} Started step worker", ExecutionConfig.ExecutionId, stepId);
            try
            {
                // Wait for the step to finish.
                await task;
            }
            finally
            {
                // Release the semaphore once to make room for new parallel executions.
                Semaphore.Release();
                Log.Information("{ExecutionId} {StepId} Finished step execution", ExecutionConfig.ExecutionId, stepId);
            }
        }
    }
}
