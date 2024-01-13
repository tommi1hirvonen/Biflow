using Biflow.Core.Entities;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IStepOrchestratorProvider
{
    public IStepOrchestrator GetOrchestratorFor(StepExecution stepExecution);
}
