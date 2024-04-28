using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal interface IOrchestrationTracker
{
    public StepExecutionMonitor? HandleUpdate(OrchestrationUpdate value);

    public StepAction? GetStepAction();
}
