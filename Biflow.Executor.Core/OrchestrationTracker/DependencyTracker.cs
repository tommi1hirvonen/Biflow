using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class DependencyTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _dependencies = [];
    private readonly Dictionary<StepExecution, OrchestrationStatus> _dependsOnThis = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        var (otherStep, status) = value;

        // Step cannot depend on itself. If the StepIds match, return early.
        if (otherStep.StepId == stepExecution.StepId)
        {
            return null;
        }

        // Keep track of steps that this step depends on.
        if (stepExecution.ExecutionDependencies.Any(d => d.DependantOnStepId == otherStep.StepId))
        {
            _dependencies[otherStep] = status;
            return new StepExecutionMonitor
            {
                ExecutionId = stepExecution.ExecutionId,
                StepId = stepExecution.StepId,
                MonitoredExecutionId = otherStep.ExecutionId,
                MonitoredStepId = otherStep.StepId,
                MonitoringReason = MonitoringReason.UpstreamDependency
            };
        }
        
        // No dependencies from the other step to this step.
        if (otherStep.ExecutionDependencies.All(d => d.DependantOnStepId != stepExecution.StepId))
        {
            return null;
        }
        
        // Keep track of steps that depend on this step.
        _dependsOnThis[otherStep] = status;
        return new StepExecutionMonitor
        {
            ExecutionId = stepExecution.ExecutionId,
            StepId = stepExecution.StepId,
            MonitoredExecutionId = otherStep.ExecutionId,
            MonitoredStepId = otherStep.StepId,
            MonitoringReason = MonitoringReason.DownstreamDependency
        };
    }

    public ObserverAction GetStepAction()
    {
        // If there are no dependencies that are being tracked, return early.
        if (_dependencies.Count == 0 && _dependsOnThis.Count == 0)
        {
            return Actions.Execute;
        }

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
        if (onSucceeded.Any(d1 => dependencyStatuses.Any(d2 => d2.Status == OrchestrationStatus.Failed && d2.StepId == d1))
            || onFailed.Any(d1 => dependencyStatuses.Any(d2 => d2.Status == OrchestrationStatus.Succeeded && d2.StepId == d1)))
        {
            return Actions.Fail(StepExecutionStatus.DependenciesFailed);
        }
        
        // No reason to skip this step.
        // If all the step's dependencies have been completed (success or failure), the step can be executed.
        if (_dependencies.All(d => d.Value is OrchestrationStatus.Succeeded or OrchestrationStatus.Failed))
        {
            return Actions.Execute;
        }

        // No action should be taken with this step at this time. Wait until next round.
        return Actions.Wait;
    }
}
