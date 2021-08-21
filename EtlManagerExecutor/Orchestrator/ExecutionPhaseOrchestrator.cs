using EtlManagerDataAccess.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class ExecutionPhaseOrchestrator : OrchestratorBase
    {
        public ExecutionPhaseOrchestrator(
            IExecutionConfiguration executionConfiguration,
            IStepExecutorFactory stepExecutorFactory,
            Execution execution)
            : base(executionConfiguration, stepExecutorFactory, execution) { }

        public override async Task RunAsync()
        {
            // Initialize CancellationTokenSources
            foreach (var step in Execution.StepExecutions)
                CancellationTokenSources[step.StepId] = new();

            // Group steps based on their execution phase
            var groupedSteps = Execution.StepExecutions
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

    }
}
