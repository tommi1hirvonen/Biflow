using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class ExecutionPhaseTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _execution = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        var (otherStep, status) = value;

        // Only track other steps in the same execution where the execution phase is lower.
        if (stepExecution.ExecutionId == otherStep.ExecutionId
            && stepExecution.StepId != otherStep.StepId
            && stepExecution.ExecutionPhase > otherStep.ExecutionPhase)
        {
            _execution[otherStep] = status;
        }

        // Monitors are not reported with execution phase tracking.
        return null;
    }

    public ObserverAction GetStepAction()
    {
        // Only steps with lower execution phase are captured in HandleUpdate().
        // If there are no steps being tracked, return early.
        if (_execution.Count == 0)
        {
            return Actions.Execute;
        }

        // Get statuses of previous steps according to execution phases.
        var previousStepStatuses = _execution
            .Select(p => p.Value)
            .Distinct()
            .ToArray();
        if (stepExecution.Execution.StopOnFirstError && previousStepStatuses.Any(status => status == OrchestrationStatus.Failed))
        {
            return Actions.Fail(StepExecutionStatus.Skipped, "Step was skipped because one or more steps failed and StopOnFirstError was set to true.");
        }

        if (previousStepStatuses.All(status => status is OrchestrationStatus.Succeeded or OrchestrationStatus.Failed))
        {
            return Actions.Execute;
        }

        // No action should be taken with this step at this time.
        return Actions.Wait;
    }
}
