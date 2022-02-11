using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestratorFactory
{
    OrchestratorBase Create(Execution execution);
}
