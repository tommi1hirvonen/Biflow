using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Common;
using EtlManager.Executor.Core.StepExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EtlManager.Executor.Core.Orchestrator;

internal class ExecutionPhaseOrchestrator : OrchestratorBase
{
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

    public ExecutionPhaseOrchestrator(
        ILogger<ExecutionPhaseOrchestrator> logger,
        IExecutionConfiguration executionConfiguration,
        IStepExecutorFactory stepExecutorFactory,
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        Execution execution)
        : base(logger, executionConfiguration, stepExecutorFactory, execution)
    {
        _dbContextFactory = dbContextFactory;
    }

    public override async Task RunAsync()
    {
        // Group steps based on their execution phase
        var groupedSteps = Execution.StepExecutions
            .GroupBy(key => key.ExecutionPhase, element => element)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());

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
                await MarkUnstartedStepsAsSkipped("Step was skipped because one or more steps failed and StopOnFirstError was set to true.");
                break;
            }
        }
    }

    private async Task MarkUnstartedStepsAsSkipped(string errorMessage)
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
            attempt.ErrorMessage = errorMessage;
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

}
