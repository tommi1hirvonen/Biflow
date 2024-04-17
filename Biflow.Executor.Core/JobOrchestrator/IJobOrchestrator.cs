namespace Biflow.Executor.Core.JobOrchestrator;

internal interface IJobOrchestrator
{
    public Task RunAsync(CancellationToken cancellationToken);

    public void CancelExecution(string username);

    public void CancelExecution(string username, Guid stepId);
}