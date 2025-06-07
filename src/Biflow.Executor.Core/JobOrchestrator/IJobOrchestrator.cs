namespace Biflow.Executor.Core.JobOrchestrator;

internal interface IJobOrchestrator
{
    public Task RunAsync(OrchestrationContext context, CancellationToken cancellationToken);

    public void CancelExecution(string username);

    public void CancelExecution(string username, Guid stepId);
}