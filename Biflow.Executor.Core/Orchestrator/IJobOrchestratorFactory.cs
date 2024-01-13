using Biflow.Core.Entities;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IJobOrchestratorFactory
{
    IJobOrchestrator Create(Execution execution);
}
