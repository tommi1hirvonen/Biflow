using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestratorFactory
{
    JobOrchestrator Create(Execution execution);
}
