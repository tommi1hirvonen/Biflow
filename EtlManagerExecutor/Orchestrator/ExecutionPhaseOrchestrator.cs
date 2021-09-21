using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class ExecutionPhaseOrchestrator : OrchestratorBase
    {
        private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

        public ExecutionPhaseOrchestrator(
            IExecutionConfiguration executionConfiguration,
            IStepExecutorFactory stepExecutorFactory,
            IDbContextFactory<EtlManagerContext> dbContextFactory,
            Execution execution)
            : base(executionConfiguration, stepExecutorFactory, execution)
        {
            _dbContextFactory = dbContextFactory;
        }

        public override async Task RunAsync()
        {
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

                // If StopOnFirstError was set to true and there are any errors,
                // mark remaining steps as skipped and stop orchestration.
                if (Execution.StopOnFirstError && StepStatuses.Any(s => s.Value == ExecutionStatus.Failed))
                {
                    await MarkUnstartedStepsAsSkipped();
                    break;
                }
            }
        }

        private async Task MarkUnstartedStepsAsSkipped()
        {
            using var context = _dbContextFactory.CreateDbContext();
            var unstartedAttempts = StepStatuses
                .Where(s => s.Value == ExecutionStatus.NotStarted)
                .Select(s => s.Key)
                .SelectMany(s => s.StepExecutionAttempts);
            foreach (var attempt in unstartedAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Skipped;
                attempt.StartDateTime = DateTimeOffset.Now;
                attempt.EndDateTime = DateTimeOffset.Now;
                context.Attach(attempt).State = EntityState.Modified;
            }
            await context.SaveChangesAsync();
        }

    }
}
