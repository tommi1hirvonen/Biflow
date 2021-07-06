using Dapper;
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
            var allSteps = await ReadStepsAsync();

            // Initialize CancellationTokenSources
            allSteps.ForEach(step => CancellationTokenSources[step.StepId] = new());

            // Group steps based on their execution phase
            var groupedSteps = allSteps
                .GroupBy(key => key.ExecutionPhase, element => element)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());

            // Start listening for cancel commands.
            RegisterCancelListeners();

            // Start all steps in each execution phase in parallel.
            foreach (var stepGroup in groupedSteps.OrderBy(g => g.Key)) // Sort step groups based on execution phase
            {
                var stepsToExecute = stepGroup.Value;
                var stepWorkers = stepsToExecute.Select(step => StartNewStepWorkerAsync(step));

                // All steps have been started. Wait until all step worker tasks have finished.
                await Task.WhenAll(stepWorkers);
            }
        }

        private async Task<List<ExecutionPhaseStep>> ReadStepsAsync()
        {
            using var sqlConnection = new SqlConnection(ExecutionConfig.ConnectionString);
            var steps = (await sqlConnection.QueryAsync<ExecutionPhaseStep>(
                "SELECT DISTINCT StepId, StepName, ExecutionPhase FROM etlmanager.Execution WHERE ExecutionId = @ExecutionId",
                new { ExecutionConfig.ExecutionId })).ToList();
            return steps;
        }
    }
}
