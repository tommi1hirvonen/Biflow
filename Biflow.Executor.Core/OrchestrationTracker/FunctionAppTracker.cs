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

        var (step, status) = value;
        
        if (step is not FunctionStepExecution functionStep)
        {
            return null;
        }

        // The other step is actually the same step.
        if (functionStep.StepId == stepExecution.StepId && functionStep.ExecutionId == stepExecution.ExecutionId)
        {
            return null;
        }

        if (functionStep.FunctionAppId == stepExecution.FunctionAppId)
        {
            _others[functionStep] = status;
            return new()
            {
                ExecutionId = stepExecution.ExecutionId,
                StepId = stepExecution.StepId,
                MonitoredExecutionId = step.ExecutionId,
                MonitoredStepId = step.StepId,
                MonitoringReason = MonitoringReason.CommonFunctionApp
            };
        }

        return null;
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