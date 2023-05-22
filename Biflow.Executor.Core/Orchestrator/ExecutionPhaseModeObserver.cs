using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal class ExecutionPhaseModeObserver : OrchestrationObserver
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _duplicates = new();
    private readonly Dictionary<StepExecution, OrchestrationStatus> _execution = new();

    public ExecutionPhaseModeObserver(
        StepExecution stepExecution,
        IStepProcessingListener orchestrationListener,
        ExtendedCancellationTokenSource cancellationTokenSource)
        : base(stepExecution, orchestrationListener, cancellationTokenSource)
    {
    }

    public override void RegisterInitialUpdates(IEnumerable<OrchestrationUpdate> initialStatuses)
    {
        foreach (var status in initialStatuses)
        {
            HandleUpdate(status);
        }
        CheckExecutionEligibility();
    }


    public override void OnUpdate(OrchestrationUpdate value)
    {
        HandleUpdate(value);
        CheckExecutionEligibility();
    }


    private void HandleUpdate(OrchestrationUpdate value)
    {
        var (step, status) = value;
        if (step.StepId == StepExecution.StepId && step.ExecutionId != StepExecution.ExecutionId)
        {
            _duplicates[step] = status;
        }
        else if (StepExecution.ExecutionId == step.ExecutionId && StepExecution.StepId != step.StepId)
        {
            _execution[step] = status;
        }
    }

    private void CheckExecutionEligibility()
    {
        var action = CalculateStepAction();
        if (action is not null)
        {
            SetResult(action);
        }
    }

    private StepAction? CalculateStepAction()
    {
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

        var previousSteps = _execution.Where(p => p.Key.ExecutionPhase < StepExecution.ExecutionPhase);
        if (StepExecution.Execution.StopOnFirstError && previousSteps.Any(s => s.Value == OrchestrationStatus.Failed))
        {
            return new Fail(StepExecutionStatus.Skipped, "Step was skipped because one or more steps failed and StopOnFirstError was set to true.");
        }

        if (previousSteps.All(s => s.Value == OrchestrationStatus.Succeeded || s.Value == OrchestrationStatus.Failed))
        {
            return new Execute();
        }

        // No action should be taken with this step at this time. Wait until next round.
        return null;
    }
}
