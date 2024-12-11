namespace Biflow.Ui.Core;

public interface IExecutorService
{
    public Task StartExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);

    public Task StopExecutionAsync(Guid executionId, Guid stepId, string username);

    public Task StopExecutionAsync(Guid executionId, string username);
    
    public Task ClearTokenCacheAsync(Guid azureCredentialId, CancellationToken cancellationToken = default);
}
