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

        private CancellationTokenSource CancellationTokenSource { get; } = new();

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
                    var stepId = reader["StepId"].ToString();
                    var executionPhase = (int)reader["ExecutionPhase"];
                    allSteps.Add(new(executionPhase, stepId));
                }
            }

            List<int> executionPhases = allSteps.Select(step => step.Key).Distinct().ToList();
            executionPhases.Sort();

            // Start listening for cancel key press.
            _ = Task.Run(ReadCancel);

            // Start all steps in each execution phase in parallel.
            foreach (int executionPhase in executionPhases)
            {
                // Get a list of steps for this execution phase.
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

        private void ReadCancel()
        {
            Console.WriteLine("Press c to cancel execution");
            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey();
            } while (cki.KeyChar != 'c');

            Console.WriteLine("Canceling all step executions");
            CancellationTokenSource.Cancel();
        }

        private async Task StartNewStepWorkerAsync(string stepId)
        {
            // Wait until the semaphore can be entered and the step can be started.
            await Semaphore.WaitAsync();
            // Create a new step worker and start it asynchronously.
            var task = new StepWorker(ExecutionConfig, stepId).ExecuteStepAsync(CancellationTokenSource.Token);
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
