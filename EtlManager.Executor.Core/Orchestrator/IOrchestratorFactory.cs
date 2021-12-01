using EtlManager.DataAccess.Models;

namespace EtlManager.Executor.Core.Orchestrator;

internal interface IOrchestratorFactory
{
    OrchestratorBase Create(Execution execution);
}
