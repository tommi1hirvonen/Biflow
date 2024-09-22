using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class TargetTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Dictionary<StepExecution, OrchestrationStatus> _writers = [];
    private readonly HashSet<Guid> _targets = stepExecution.DataObjects
        .Where(o => o.ReferenceType == DataObjectReferenceType.Target)
        .Select(o => o.ObjectId)
        .ToHashSet();

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        // If there are no targets for this step, no need to track steps.
        if (_targets.Count == 0)
        {
            return null;
        }

        var (step, status) = value;

        if ((step.StepId != stepExecution.StepId || step.ExecutionId != stepExecution.ExecutionId)
            && step.DataObjects.Any(o => o.ReferenceType == DataObjectReferenceType.Target && _targets.Contains(o.ObjectId)))
        {
            _writers[step] = status;
            return new()
            {
                ExecutionId = stepExecution.ExecutionId,
                StepId = stepExecution.StepId,
                MonitoredExecutionId = step.ExecutionId,
                MonitoredStepId = step.StepId,
                MonitoringReason = MonitoringReason.CommonTarget
            };
        }

        return null;
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
        var limits = stepExecution.DataObjects
            .Where(o => o.ReferenceType == DataObjectReferenceType.Target)
            .ToDictionary(o => o.ObjectId, o => o.DataObject.MaxConcurrentWrites);
        
        foreach (var (target, count) in targets)
        {
            var limit = limits.GetValueOrDefault(target, 0);
            if (limit > 0 && count >= limit)
            {
                return Actions.Wait;
            }
        }

        return Actions.Execute;
    }
}
