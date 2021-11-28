using EtlManager.DataAccess.Models;

namespace EtlManager.Executor.Core.Orchestrator;

public interface IOrchestratorFactory
{
    OrchestratorBase Create(Execution execution);
}
