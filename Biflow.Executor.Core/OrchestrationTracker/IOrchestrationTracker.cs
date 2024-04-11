using Biflow.Executor.Core.Orchestrator;

namespace Biflow.Executor.Core.OrchestrationTracker;

internal interface IOrchestrationTracker
{
    public void HandleUpdate(OrchestrationUpdate update);

    public StepAction? GetStepAction();
}
