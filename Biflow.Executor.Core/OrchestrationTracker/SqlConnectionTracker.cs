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

    private readonly int maxConcurrentSteps = stepExecution switch
    {
        SqlStepExecution sql when sql.GetConnection() is MsSqlConnection { MaxConcurrentSqlSteps: >= 0 } ms => ms.MaxConcurrentSqlSteps,
        SqlStepExecution sql when sql.GetConnection() is SnowflakeConnection { MaxConcurrentSqlSteps: >= 0 } sf => sf.MaxConcurrentSqlSteps,
        PackageStepExecution package when package.GetConnection() is MsSqlConnection { MaxConcurrentPackageSteps: >= 0 } ms => ms.MaxConcurrentPackageSteps,
        _ => 0
    };

    private readonly Dictionary<StepExecution, OrchestrationStatus> _others = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        // Check whether there's a need to monitor and return early if not.
        if (maxConcurrentSteps <= 0)
        {
            return null;
        }

        var (step, status) = value;

        // The other step is actually the same step.
        if (step.StepId == stepExecution.StepId && step.ExecutionId == stepExecution.ExecutionId)
        {
            return null;
        }

        // Only track steps of the same type to not mix Sql and Package step concurrencies.
        if (step.StepType != stepExecution.StepType)
        {
            return null;
        }

        var connectionId = step switch
        {
            SqlStepExecution sql => sql.GetConnection()?.ConnectionId,
            PackageStepExecution package => package.GetConnection()?.ConnectionId,
            _ => null
        };

        if (connectionId is Guid g1 && _connectionId is Guid g2 && g1 == g2)
        {
            _others[step] = status;
            return new()
            {
                ExecutionId = stepExecution.ExecutionId,
                StepId = stepExecution.StepId,
                MonitoredExecutionId = step.ExecutionId,
                MonitoredStepId = step.StepId,
                MonitoringReason = MonitoringReason.CommonConnection
            };
        }

        return null;
    }

    public ObserverAction GetStepAction()
    {
        if (_connectionId is null || maxConcurrentSteps <= 0)
        {
            return Actions.Execute;
        }

        var runningCount = _others.Count(o => o.Value == OrchestrationStatus.Running);

        if (runningCount >= maxConcurrentSteps)
        {
            return Actions.Wait;
        }

        return Actions.Execute;
    }
}
