using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class SqlConnectionTracker(StepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Guid? _connectionId = stepExecution switch
    {
        SqlStepExecution sql => sql.GetConnection()?.ConnectionId,
        PackageStepExecution package => package.GetConnection()?.ConnectionId,
        _ => null
    };

    private readonly int _maxConcurrentSteps = stepExecution switch
    {
        SqlStepExecution sql when sql.GetConnection() is { MaxConcurrentSqlSteps: >= 0 } ms => ms.MaxConcurrentSqlSteps,
        PackageStepExecution package when package.GetConnection() is { MaxConcurrentPackageSteps: >= 0 } ms => ms.MaxConcurrentPackageSteps,
        _ => 0
    };

    private readonly Dictionary<StepExecution, OrchestrationStatus> _others = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        // Check whether there's a need to monitor and return early if not.
        if (_maxConcurrentSteps <= 0)
        {
            return null;
        }

        var (otherStep, status) = value;

        // The other step is actually the same step.
        if (otherStep.StepId == stepExecution.StepId && otherStep.ExecutionId == stepExecution.ExecutionId)
        {
            return null;
        }

        // Only track steps of the same type to not mix Sql and Package step concurrences.
        if (otherStep.StepType != stepExecution.StepType)
        {
            return null;
        }

        var otherConnectionId = otherStep switch
        {
            SqlStepExecution sql => sql.GetConnection()?.ConnectionId,
            PackageStepExecution package => package.GetConnection()?.ConnectionId,
            _ => null
        };

        // The connections are not the same.
        if (otherConnectionId is not { } id1 || _connectionId is not { } id2 || id1 != id2)
        {
            return null;
        }
        
        _others[otherStep] = status;
        return new()
        {
            ExecutionId = stepExecution.ExecutionId,
            StepId = stepExecution.StepId,
            MonitoredExecutionId = otherStep.ExecutionId,
            MonitoredStepId = otherStep.StepId,
            MonitoringReason = MonitoringReason.CommonConnection
        };
    }

    public ObserverAction GetStepAction()
    {
        if (_connectionId is null || _maxConcurrentSteps <= 0)
        {
            return Actions.Execute;
        }

        var runningCount = _others.Count(o => o.Value == OrchestrationStatus.Running);

        if (runningCount >= _maxConcurrentSteps)
        {
            return Actions.Wait;
        }

        return Actions.Execute;
    }
}
