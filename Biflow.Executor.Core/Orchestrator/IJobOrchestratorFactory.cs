using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IJobOrchestratorFactory
{
    IJobOrchestrator Create(Execution execution);
}
