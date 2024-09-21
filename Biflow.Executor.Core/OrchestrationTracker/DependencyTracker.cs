using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class DependencyTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _dependencies = [];
    private readonly Dictionary<StepExecution, OrchestrationStatus> _dependsOnThis = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        var (step, status) = value;
        // Keep track of steps that this step depends on.
        if (stepExecution.ExecutionDependencies.Any(d => d.DependantOnStepId == step.StepId))
        {
            _dependencies[step] = status;
            return new()
            {
                ExecutionId = stepExecution.ExecutionId,
                StepId = stepExecution.StepId,
                MonitoredExecutionId = step.ExecutionId,
                MonitoredStepId = step.StepId,
                MonitoringReason = MonitoringReason.UpstreamDependency
            };
        }
        // Keep track of steps that depend on this step.
        else if (step.ExecutionDependencies.Any(d => d.DependantOnStepId == stepExecution.StepId))
        {
            _dependsOnThis[step] = status;
            return new()
            {
                ExecutionId = stepExecution.ExecutionId,
                StepId = stepExecution.StepId,
                MonitoredExecutionId = step.ExecutionId,
                MonitoredStepId = step.StepId,
                MonitoringReason = MonitoringReason.DownstreamDependency
            };
        }
        return null;
    }

    public ObserverAction GetStepAction()
    {
        // If there are running steps that depend on this step => wait.
        if (_dependsOnThis.Any(d => d.Value == OrchestrationStatus.Running))
        {
            return Actions.Wait;
        }

        var onSucceeded = stepExecution.ExecutionDependencies
            .Where(d => d.DependencyType == DependencyType.OnSucceeded)
            .Select(d => d.DependantOnStepId);

        var onFailed = stepExecution.ExecutionDependencies
            .Where(d => d.DependencyType == DependencyType.OnFailed)
            .Select(d => d.DependantOnStepId);

        var dependencyStatuses = _dependencies.Select(d => new { d.Key.StepId, Status = d.Value });

        // If there are any on-success dependencies, which have been marked as failed
        // OR
        // if there are any on-failed dependencies, which have been marked as succeeded, skip this step.
        if (onSucceeded.Any(d1 => dependencyStatuses.Any(d2 => d2.Status == OrchestrationStatus.Failed && d2.StepId == d1)) ||
            onFailed.Any(d1 => dependencyStatuses.Any(d2 => d2.Status == OrchestrationStatus.Succeeded && d2.StepId == d1)))
        {
            return Actions.Fail(StepExecutionStatus.DependenciesFailed);
        }
        // No reason to skip this step.
        // If all the step's dependencies have been completed (success or failure), the step can be executed.
        else if (_dependencies.All(d => d.Value == OrchestrationStatus.Succeeded || d.Value == OrchestrationStatus.Failed))
        {
            return Actions.Execute;
        }

        // No action should be taken with this step at this time. Wait until next round.
        return Actions.Wait;
    }
}
