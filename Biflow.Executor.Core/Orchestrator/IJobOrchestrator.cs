namespace Biflow.Executor.Core.Orchestrator;

internal interface IJobOrchestrator
{
    public Task RunAsync();

    public void CancelExecution(string username);

    public void CancelExecution(string username, Guid stepId);
}