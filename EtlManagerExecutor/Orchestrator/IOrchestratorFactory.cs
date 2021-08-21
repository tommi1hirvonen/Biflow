using EtlManagerDataAccess.Models;

namespace EtlManagerExecutor
{
    public interface IOrchestratorFactory
    {
        OrchestratorBase Create(Execution execution);
    }
}