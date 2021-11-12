using EtlManager.DataAccess.Models;

namespace EtlManager.Executor;

public interface IOrchestratorFactory
{
    OrchestratorBase Create(Execution execution);
}
