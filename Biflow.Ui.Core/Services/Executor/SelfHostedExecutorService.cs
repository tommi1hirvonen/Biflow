using Biflow.Executor.Core;
using Biflow.Executor.Core.FilesExplorer;

namespace Biflow.Ui.Core;

public class SelfHostedExecutorService(
    IExecutionManager executionManager, ITokenService tokenService) : IExecutorService
{
    public async Task StartExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        await executionManager.StartExecutionAsync(executionId, cancellationToken);
    }

    public Task StopExecutionAsync(Guid executionId, Guid stepId, string username)
    {
        executionManager.CancelExecution(executionId, username, stepId);
        return Task.CompletedTask;
    }

    public Task StopExecutionAsync(Guid executionId, string username)
    {
        executionManager.CancelExecution(executionId, username);
        return Task.CompletedTask;
    }
    
    public Task ClearTokenCacheAsync(Guid azureCredentialId, CancellationToken cancellationToken = default) =>
        tokenService.ClearAsync(azureCredentialId, cancellationToken);

    public Task<IReadOnlyList<DirectoryItem>> GetDirectoryItemsAsync(string? path,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DirectoryItem> items = FileExplorer.GetDirectoryItems(path);
        return Task.FromResult(items);
    }
}
