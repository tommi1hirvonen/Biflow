using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal class ExecutionPhaseModeObserver(
    StepExecution stepExecution,
    IStepExecutionListener orchestrationListener,
    ExtendedCancellationTokenSource cancellationTokenSource)
    : OrchestrationObserver(stepExecution, orchestrationListener, cancellationTokenSource)
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _duplicates = [];
    private readonly Dictionary<StepExecution, OrchestrationStatus> _execution = [];

    protected override void HandleUpdate(OrchestrationUpdate value)
    {
        var (step, status) = value;
        // Keep track of the same being possibly executed in a different execution.
        if (step.StepId == StepExecution.StepId && step.ExecutionId != StepExecution.ExecutionId)
        {
            _duplicates[step] = status;
        }
        // Also keep track of other step executions in the same execution.
        else if (StepExecution.ExecutionId == step.ExecutionId && StepExecution.StepId != step.StepId)
        {
            _execution[step] = status;
        }
    }

    protected override StepAction? GetStepAction()
    {
        // First check for duplicate executions.
        if (_duplicates.Any(d => d.Value == OrchestrationStatus.Running) &&
            StepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Fail)
        {
            return new Fail(StepExecutionStatus.Duplicate);
        }

        if (_duplicates.Any(d => d.Value == OrchestrationStatus.Running) &&
            StepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Wait)
        {
            return null;
        }

        // Get statuses of previous steps according to execution phases.
        var previousStepStatuses = _execution
            .Where(p => p.Key.ExecutionPhase < StepExecution.ExecutionPhase)
            .Select(p => p.Value)
            .Distinct()
            .ToArray();
        if (StepExecution.Execution.StopOnFirstError && previousStepStatuses.Any(status => status == OrchestrationStatus.Failed))
        {
            return new Fail(StepExecutionStatus.Skipped, "Step was skipped because one or more steps failed and StopOnFirstError was set to true.");
        }

        if (previousStepStatuses.All(status => status == OrchestrationStatus.Succeeded || status == OrchestrationStatus.Failed))
        {
            return new Execute();
        }

        // No action should be taken with this step at this time. Wait until next round.
        return null;
    }
}
