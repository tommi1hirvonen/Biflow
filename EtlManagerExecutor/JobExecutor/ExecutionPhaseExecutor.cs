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
            var allSteps = new List<KeyValuePair<int, Step>>();

            using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
            await sqlConnection.OpenAsync();
            SqlCommand sqlCommand = new SqlCommand("SELECT DISTINCT StepId, StepName, ExecutionPhase FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId", sqlConnection);
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

            // Start listening for cancel key press from the console.
            _ = Task.Run(ReadCancelKey);
            // Start listening for cancel command from the UI application.
            _ = Task.Run(() => ReadCancelPipe(ExecutionConfig.ExecutionId));

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

        private async Task StartNewStepWorkerAsync(Step step)
        {
            // Wait until the semaphore can be entered and the step can be started.
            await Semaphore.WaitAsync();
            // Create a new step worker and start it asynchronously.
            var token = CancellationTokenSources[step.StepId].Token;
            var task = new StepWorker(ExecutionConfig, step).ExecuteStepAsync(token);
            Log.Information("{ExecutionId} {step} Started step worker", ExecutionConfig.ExecutionId, step);
            try
            {
                // Wait for the step to finish.
                await task;
            }
            finally
            {
                // Release the semaphore once to make room for new parallel executions.
                Semaphore.Release();
                Log.Information("{ExecutionId} {step} Finished step execution", ExecutionConfig.ExecutionId, step);
            }
        }
    }
}
