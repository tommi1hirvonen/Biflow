using Biflow.Core.Entities.Steps.Execution;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal class DependencyModeObserver(
    StepExecution stepExecution,
    IStepExecutionListener orchestrationListener,
    ExtendedCancellationTokenSource cancellationTokenSource)
    : OrchestrationObserver(stepExecution, orchestrationListener, cancellationTokenSource)
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _dependencies = [];
    private readonly Dictionary<StepExecution, OrchestrationStatus> _dependsOnThis = [];
    private readonly Dictionary<StepExecution, OrchestrationStatus> _duplicates = [];

    protected override void HandleUpdate(OrchestrationUpdate value)
    {
        var (step, status) = value;
        // Keep track of the same being possibly executed in a different execution.
        if (step.StepId == StepExecution.StepId && step.ExecutionId != StepExecution.ExecutionId)
        {
            _duplicates[step] = status;
        }
        // Keep track of steps that this step depends on.
        else if (StepExecution.ExecutionDependencies.Any(d => d.DependantOnStepId == step.StepId))
        {
            _dependencies[step] = status;
        }
        // Keep track of steps that depend on this step.
        else if (step.ExecutionDependencies.Any(d => d.DependantOnStepId == StepExecution.StepId))
        {
            _dependsOnThis[step] = status;
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

        // If there are running steps that depend on this step => wait.
        if (_dependsOnThis.Any(d => d.Value == OrchestrationStatus.Running))
        {
            return null;
        }

        var onSucceeded = StepExecution.ExecutionDependencies
            .Where(d => d.DependencyType == DependencyType.OnSucceeded)
            .Select(d => d.DependantOnStepId);

        var onFailed = StepExecution.ExecutionDependencies
            .Where(d => d.DependencyType == DependencyType.OnFailed)
            .Select(d => d.DependantOnStepId);

        var dependencyStatuses = _dependencies.Select(d => new { d.Key.StepId, Status = d.Value });

        // If there are any on-success dependencies, which have been marked as failed
        // OR
        // if there are any on-failed dependencies, which have been marked as succeeded, skip this step.
        if (onSucceeded.Any(d1 => dependencyStatuses.Any(d2 => d2.Status == OrchestrationStatus.Failed && d2.StepId == d1)) ||
            onFailed.Any(d1 => dependencyStatuses.Any(d2 => d2.Status == OrchestrationStatus.Succeeded && d2.StepId == d1)))
        {
            return new Fail(StepExecutionStatus.DependenciesFailed);
        }
        // No reason to skip this step.
        // If all the step's dependencies have been completed (success or failure), the step can be executed.
        else if (_dependencies.All(d => d.Value == OrchestrationStatus.Succeeded || d.Value == OrchestrationStatus.Failed))
        {
            return new Execute();
        }

        // No action should be taken with this step at this time. Wait until next round.
        return null;
    }

}
