using Biflow.Core.Entities.Steps.Execution;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepOrchestratorProvider
{
    public IStepOrchestrator GetOrchestratorFor(StepExecution stepExecution);
}
