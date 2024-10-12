using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class DuplicateExecutionTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _duplicates = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        // If the duplicate execution behaviour is Allow, no need to track steps.
        if (stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Allow)
        {
            return null;
        }

        var (step, status) = value;
        // Keep track of the same step being possibly executed in a different execution.
        if (step.StepId == stepExecution.StepId && step.ExecutionId != stepExecution.ExecutionId)
        {
            _duplicates[step] = status;
            return new()
            {
                ExecutionId = stepExecution.ExecutionId,
                StepId = stepExecution.StepId,
                MonitoredExecutionId = step.ExecutionId,
                MonitoredStepId = step.StepId,
                MonitoringReason = MonitoringReason.Duplicate
            };
        }
        return null;
    }

    public ObserverAction GetStepAction()
    {
        // If there are no tracked duplicates, return early.
        if (_duplicates.Count == 0)
        {
            return Actions.Execute;
        }

        // There are duplicates and the duplicate behaviour is defined as Fail.
        if (stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Fail
            && _duplicates.Any(d => d.Value == OrchestrationStatus.Running))
        {
            return Actions.Fail(StepExecutionStatus.Duplicate);
        }

        // There are duplicates and the duplicate behaviour is defined as Wait.
        if (stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Wait
            && _duplicates.Any(d => d.Value == OrchestrationStatus.Running))
        {
            return Actions.Wait;
        }

        // No duplicates or the duplicate behaviour is Allow.
        return Actions.Execute;
    }
}
