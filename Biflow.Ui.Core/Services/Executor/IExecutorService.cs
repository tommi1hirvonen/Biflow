namespace Biflow.Ui.Core;

public interface IExecutorService
{
    public Task StartExecutionAsync(Guid executionId);

    public Task StopExecutionAsync(Guid executionId, Guid stepId, string username);

    public Task StopExecutionAsync(Guid executionId, string username);
}
