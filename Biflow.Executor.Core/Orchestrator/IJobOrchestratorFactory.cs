using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IJobOrchestratorFactory
{
    JobOrchestrator Create(Execution execution);
}
