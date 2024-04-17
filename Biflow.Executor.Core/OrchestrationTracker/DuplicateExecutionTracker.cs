using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class DuplicateExecutionTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _duplicates = [];

    public void HandleUpdate(OrchestrationUpdate value)
    {
        var (step, status) = value;
        // Keep track of the same being possibly executed in a different execution.
        if (step.StepId == stepExecution.StepId && step.ExecutionId != stepExecution.ExecutionId)
        {
            _duplicates[step] = status;
        }
    }

    public StepAction? GetStepAction()
    {
        // There are duplicates and the duplicate behaviour is defined as Fail.
        if (_duplicates.Any(d => d.Value == OrchestrationStatus.Running) &&
            stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Fail)
        {
            return new Fail(StepExecutionStatus.Duplicate);
        }

        // There are duplicates and the duplicate behaviour is defined as Wait.
        if (_duplicates.Any(d => d.Value == OrchestrationStatus.Running) &&
            stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Wait)
        {
            return null;
        }

        // No duplicates or the duplicate behaviour is Allow.
        return new Execute();
    }
}
