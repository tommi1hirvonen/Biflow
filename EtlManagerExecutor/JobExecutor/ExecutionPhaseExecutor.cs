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
    class ExecutionPhaseExecutor
    {
        private ExecutionConfiguration ExecutionConfig { get; init; }

        private int RunningStepsCounter = 0;

        public ExecutionPhaseExecutor(ExecutionConfiguration executionConfiguration)
        {
            ExecutionConfig = executionConfiguration;
        }

        public async Task RunAsync()
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
                    // Check whether the maximum number of parallel steps are running
                    // and wait for some steps to finish if necessary.
                    while (RunningStepsCounter >= ExecutionConfig.MaxParallelSteps)
                    {
                        await Task.Delay(ExecutionConfig.PollingIntervalMs);
                    }

                    stepWorkers.Add(StartNewStepWorkerAsync(stepId));

                    Log.Information("{ExecutionId} {stepId} Started step worker", ExecutionConfig.ExecutionId, stepId);

                }

                // All steps have been started. Wait until all step worker tasks have finished.
                await Task.WhenAll(stepWorkers);
            }
        }

        private async Task StartNewStepWorkerAsync(string stepId)
        {
            // Create a new step worker and start it asynchronously.
            var task = new StepWorker(ExecutionConfig, stepId).ExecuteStepAsync();
            // Add one to the counter.
            Interlocked.Increment(ref RunningStepsCounter);
            try
            {
                // Wait for the step to finish.
                await task;
            }
            finally
            {
                // Subtract one from the counter.
                Interlocked.Decrement(ref RunningStepsCounter);
                Log.Information("{ExecutionId} {StepId} Finished step execution", ExecutionConfig.ExecutionId, stepId);
            }
        }
    }
}
