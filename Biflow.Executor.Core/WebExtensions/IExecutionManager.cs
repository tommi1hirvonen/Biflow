namespace Biflow.Executor.Core.WebExtensions;

public interface IExecutionManager
{
    public void CancelExecution(Guid executionId, string username);
    
    public void CancelExecution(Guid executionId, string username, Guid stepId);
    
    public bool IsExecutionRunning(Guid executionId);
    
    public Task StartExecutionAsync(Guid executionId);
    
    public Task WaitForTaskCompleted(Guid executionId, CancellationToken cancellationToken);
}