using Biflow.Executor.Core;
using Biflow.ExecutorProxy.Core.FilesExplorer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Biflow.Ui.Core;

public class SelfHostedExecutorService(
    IExecutionManager executionManager,
    ITokenService tokenService,
    HealthCheckService healthCheckService) : IExecutorService
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
    
    public async Task<HealthReportDto> GetHealthReportAsync(CancellationToken cancellationToken = default)
    {
        // When running executor in self-hosted mode, only run health checks for the executor service
        // (tag == "executor").
        var healthReport = await healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("executor"),
            cancellationToken);
        return new HealthReportDto(healthReport);
    }
}
