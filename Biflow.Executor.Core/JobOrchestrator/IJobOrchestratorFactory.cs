namespace Biflow.Executor.Core.JobOrchestrator;

internal interface IJobOrchestratorFactory
{
    IJobOrchestrator Create(Execution execution);
}
