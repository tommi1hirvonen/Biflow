using Dapper;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
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
        public ExecutionPhaseExecutor(ExecutionConfiguration executionConfiguration, Execution execution)
            : base(executionConfiguration, execution) { }

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
