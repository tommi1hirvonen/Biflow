using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class FunctionAppTracker(FunctionStepExecution stepExecution) : IOrchestrationTracker
{
    private readonly FunctionApp? _functionApp = stepExecution.GetApp();
    private readonly Dictionary<FunctionStepExecution, OrchestrationStatus> _others = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        // Check whether there's a need to monitor and return early if not.
        if (_functionApp is null or { MaxConcurrentFunctionSteps: <= 0 })
        {
            return null;
        }

        var (otherStep, status) = value;
        
        if (otherStep is not FunctionStepExecution functionStep)
        {
            return null;
        }

        // The other step is actually the same step.
        if (functionStep.StepId == stepExecution.StepId && functionStep.ExecutionId == stepExecution.ExecutionId)
        {
            return null;
        }

        // The Function Apps are not the same.
        if (functionStep.FunctionAppId != stepExecution.FunctionAppId)
        {
            return null;
        }

        // The step is not being tracked, and it has already completed.
        if (!_others.ContainsKey(functionStep) && status is OrchestrationStatus.Succeeded or OrchestrationStatus.Failed)
        {
            return null;
        }
        
        _others[functionStep] = status;
        return new StepExecutionMonitor
        {
            ExecutionId = stepExecution.ExecutionId,
            StepId = stepExecution.StepId,
            MonitoredExecutionId = otherStep.ExecutionId,
            MonitoredStepId = otherStep.StepId,
            MonitoringReason = MonitoringReason.CommonFunctionApp
        };
    }

    public ObserverAction GetStepAction()
    {
        if (_functionApp is null or { MaxConcurrentFunctionSteps: <= 0 })
        {
            return Actions.Execute;
        }

        var runningCount = _others.Count(o => o.Value == OrchestrationStatus.Running);

        if (runningCount >= _functionApp.MaxConcurrentFunctionSteps)
        {
            return Actions.Wait;
        }

        return Actions.Execute;
    }
}