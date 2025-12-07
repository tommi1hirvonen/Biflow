using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class ProxyTracker(ExeStepExecution stepExecution) : IOrchestrationTracker
{
    private readonly Proxy? _proxy = stepExecution.GetProxy();
    private readonly Dictionary<ExeStepExecution, OrchestrationStatus> _others = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        // Check whether there's a need to monitor (step uses proxy with concurrency limit) and return early if not.
        if (_proxy is null or { MaxConcurrentExeSteps: <= 0 })
        {
            return null;
        }

        var (otherStep, status) = value;
        
        if (otherStep is not ExeStepExecution exeStep)
        {
            return null;
        }

        // The other step is actually the same step.
        if (exeStep.StepId == stepExecution.StepId && exeStep.ExecutionId == stepExecution.ExecutionId)
        {
            return null;
        }

        // The proxies are different.
        if (exeStep.ProxyId != stepExecution.ProxyId)
        {
            return null;
        }

        // The step is not being tracked, and it has already completed.
        if (!_others.ContainsKey(exeStep) && status is OrchestrationStatus.Succeeded or OrchestrationStatus.Failed)
        {
            return null;
        }
        
        _others[exeStep] = status;
        return new StepExecutionMonitor
        {
            ExecutionId = stepExecution.ExecutionId,
            StepId = stepExecution.StepId,
            MonitoredExecutionId = otherStep.ExecutionId,
            MonitoredStepId = otherStep.StepId,
            MonitoringReason = MonitoringReason.CommonProxy
        };
    }

    public ObserverAction GetStepAction()
    {
        if (_proxy is null or { MaxConcurrentExeSteps: <= 0 })
        {
            return Actions.Execute;
        }

        var runningCount = _others.Count(o => o.Value == OrchestrationStatus.Running);

        if (runningCount >= _proxy.MaxConcurrentExeSteps)
        {
            return Actions.Wait;
        }

        return Actions.Execute;
    }
}