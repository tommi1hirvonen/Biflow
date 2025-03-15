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

        var (otherStep, status) = value;
        
        // The step is different, or it is the same execution.
        if (otherStep.StepId != stepExecution.StepId || otherStep.ExecutionId == stepExecution.ExecutionId)
        {
            return null;
        }
        
        // Keep track of the same step being possibly executed in a different execution.
        _duplicates[otherStep] = status;
        return new StepExecutionMonitor
        {
            ExecutionId = stepExecution.ExecutionId,
            StepId = stepExecution.StepId,
            MonitoredExecutionId = otherStep.ExecutionId,
            MonitoredStepId = otherStep.StepId,
            MonitoringReason = MonitoringReason.Duplicate
        };
    }

    public ObserverAction GetStepAction()
    {
        // If there are no tracked duplicates, return early.
        if (_duplicates.Count == 0)
        {
            return Actions.Execute;
        }

        return stepExecution.DuplicateExecutionBehaviour switch
        {
            // There are duplicates and the duplicate behaviour is defined as Fail.
            DuplicateExecutionBehaviour.Fail when _duplicates.Any(d => d.Value == OrchestrationStatus.Running) =>
                Actions.Fail(StepExecutionStatus.Duplicate),
            // There are duplicates and the duplicate behaviour is defined as Wait.
            DuplicateExecutionBehaviour.Wait when _duplicates.Any(d => d.Value == OrchestrationStatus.Running) =>
                Actions.Wait,
            // No duplicates or the duplicate behaviour is Allow.
            _ =>
                Actions.Execute
        };
    }
}
