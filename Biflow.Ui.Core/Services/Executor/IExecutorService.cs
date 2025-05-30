﻿using Biflow.ExecutorProxy.Core.FilesExplorer;
using Microsoft.Extensions.Hosting;

namespace Biflow.Ui.Core;

public interface IExecutorService
{
    public Task StartExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);

    public Task StopExecutionAsync(Guid executionId, Guid stepId, string username);

    public Task StopExecutionAsync(Guid executionId, string username);
    
    public Task ClearTokenCacheAsync(Guid azureCredentialId, CancellationToken cancellationToken = default);
    
    public Task<IReadOnlyList<DirectoryItem>> GetDirectoryItemsAsync(string? path,
        CancellationToken cancellationToken = default);
    
    public Task<HealthReportDto> GetHealthReportAsync(CancellationToken cancellationToken = default);

    public Task ClearTransientHealthErrorsAsync(CancellationToken cancellationToken = default);
}
