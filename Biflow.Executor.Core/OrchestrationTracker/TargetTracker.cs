using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class TargetTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _writers = [];
    private readonly HashSet<Guid> _targets = stepExecution.DataObjects
        .Where(o => o.ReferenceType == DataObjectReferenceType.Target)
        .Select(o => o.ObjectId)
        .ToHashSet();
    private readonly Dictionary<Guid, int> _limits = stepExecution.DataObjects
        .Where(o => o.ReferenceType == DataObjectReferenceType.Target)
        .ToDictionary(o => o.ObjectId, o => o.DataObject.MaxConcurrentWrites);

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        // If there are no targets for this step, no need to track steps.
        if (_targets.Count == 0)
        {
            return null;
        }

        var (otherStep, status) = value;
        
        // The other step is actually the same step.
        if (otherStep.StepId == stepExecution.StepId && otherStep.ExecutionId == stepExecution.ExecutionId)
        {
            return null;
        }
        
        // The step is not being tracked, and it has already completed.
        if (!_writers.ContainsKey(otherStep) && status is OrchestrationStatus.Succeeded or OrchestrationStatus.Failed)
        {
            return null;
        }

        // The other step has no common targets.
        if (!otherStep.DataObjects.Any(o =>
                o.ReferenceType == DataObjectReferenceType.Target && _targets.Contains(o.ObjectId)))
        {
            return null;
        }
        
        _writers[otherStep] = status;
        return new StepExecutionMonitor
        {
            ExecutionId = stepExecution.ExecutionId,
            StepId = stepExecution.StepId,
            MonitoredExecutionId = otherStep.ExecutionId,
            MonitoredStepId = otherStep.StepId,
            MonitoringReason = MonitoringReason.CommonTarget
        };
    }

    public ObserverAction GetStepAction()
    {
        // If there are no targets or no steps currently writing to the targets, return early.
        if (_targets.Count == 0 || _writers.Count == 0)
        {
            return Actions.Execute;
        }

        var targets = _writers
            .Where(w => w.Value == OrchestrationStatus.Running)
            .SelectMany(w => w.Key.DataObjects.Where(o => o.ReferenceType == DataObjectReferenceType.Target && _targets.Contains(o.ObjectId)))
            .GroupBy(o => o.ObjectId)
            .Select(g => (Target: g.Key, Count: g.Count()));
        
        foreach (var (target, count) in targets)
        {
            var limit = _limits.GetValueOrDefault(target, 0);
            if (limit > 0 && count >= limit)
            {
                return Actions.Wait;
            }
        }

        return Actions.Execute;
    }
}
