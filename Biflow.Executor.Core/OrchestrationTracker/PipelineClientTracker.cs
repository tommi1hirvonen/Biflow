using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal class PipelineClientTracker(PipelineStepExecution stepExecution) : IOrchestrationTracker
{
    private readonly PipelineClient? _client = stepExecution.GetClient();
    private readonly Dictionary<PipelineStepExecution, OrchestrationStatus> _others = [];

    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value)
    {
        // Check whether there's a need to monitor and return early if not.
        if (_client is null or { MaxConcurrentPipelineSteps: <= 0 })
        {
            return null;
        }

        var (step, status) = value;

        if (step is not PipelineStepExecution pipelineStep)
        {
            return null;
        }

        // The other step is actually the same step.
        if (pipelineStep.StepId == stepExecution.StepId && pipelineStep.ExecutionId == stepExecution.ExecutionId)
        {
            return null;
        }

        if (pipelineStep.PipelineClientId == stepExecution.PipelineClientId)
        {
            _others[pipelineStep] = status;
            return new()
            {
                ExecutionId = stepExecution.ExecutionId,
                StepId = stepExecution.StepId,
                MonitoredExecutionId = step.ExecutionId,
                MonitoredStepId = step.StepId,
                MonitoringReason = MonitoringReason.CommonPipelineClient
            };
        }

        return null;
    }

    public ObserverAction GetStepAction()
    {
        if (_client is null or { MaxConcurrentPipelineSteps: <= 0 })
        {
            return Actions.Execute;
        }

        var runningCount = _others.Count(o => o.Value == OrchestrationStatus.Running);

        if (runningCount >= _client.MaxConcurrentPipelineSteps)
        {
            return Actions.Wait;
        }

        return Actions.Execute;
    }
}
