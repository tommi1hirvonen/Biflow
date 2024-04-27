using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class ExecutionPhaseTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _execution = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        var (step, status) = value;
        if (stepExecution.ExecutionId == step.ExecutionId && stepExecution.StepId != step.StepId)
        {
            _execution[step] = status;
        }
        return null;
    }

    public StepAction? GetStepAction()
    {
        // Get statuses of previous steps according to execution phases.
        var previousStepStatuses = _execution
            .Where(p => p.Key.ExecutionPhase < stepExecution.ExecutionPhase)
            .Select(p => p.Value)
            .Distinct()
            .ToArray();
        if (stepExecution.Execution.StopOnFirstError && previousStepStatuses.Any(status => status == OrchestrationStatus.Failed))
        {
            return new Fail(StepExecutionStatus.Skipped, "Step was skipped because one or more steps failed and StopOnFirstError was set to true.");
        }

        if (previousStepStatuses.All(status => status == OrchestrationStatus.Succeeded || status == OrchestrationStatus.Failed))
        {
            return new Execute();
        }

        // No action should be taken with this step at this time.
        return null;
    }
}
