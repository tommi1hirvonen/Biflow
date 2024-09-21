using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class DuplicateExecutionTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _duplicates = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        var (step, status) = value;
        // Keep track of the same being possibly executed in a different execution.
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
        // There are duplicates and the duplicate behaviour is defined as Fail.
        if (_duplicates.Any(d => d.Value == OrchestrationStatus.Running) &&
            stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Fail)
        {
            return Actions.Fail(StepExecutionStatus.Duplicate);
        }

        // There are duplicates and the duplicate behaviour is defined as Wait.
        if (_duplicates.Any(d => d.Value == OrchestrationStatus.Running) &&
            stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Wait)
        {
            return Actions.Wait;
        }

        // No duplicates or the duplicate behaviour is Allow.
        return Actions.Execute;
    }
}
