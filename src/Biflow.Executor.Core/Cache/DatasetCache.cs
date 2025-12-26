using Microsoft.Extensions.Caching.Memory;

namespace Biflow.Executor.Core.Cache;

/// <summary>
/// Represents a caching mechanism for retrieving and storing dataset IDs in a memory cache.
/// Handles synchronized access across multiple executions and workspaces.
/// </summary>
public class DatasetCache(IMemoryCache cache)
{
    // TODO Handle multiple executions and workspaces at once.
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public async Task<string?> GetDatasetIdAsync(
        DatasetClient client,
        Guid executionId,
        Guid workspaceId,
        string datasetName,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(executionId, workspaceId, datasetName);
        if (cache.TryGetValue(cacheKey, out string? cachedDatasetId))
        {
            return cachedDatasetId;
        }
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var datasets = await client.GetDatasetsAsync(workspaceId, cancellationToken);
            string? datasetId = null;
            foreach (var dataset in datasets)
            {
                var key = CacheKey(executionId, workspaceId, dataset.DatasetName);
                cache.Set(key, dataset.DatasetId, TimeSpan.FromMinutes(60));
                if (dataset.DatasetName == datasetName)
                {
                    datasetId = dataset.DatasetId;
                }
            }
            return datasetId;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private static string CacheKey(Guid executionId, Guid workspaceId, string datasetName) =>
        $"{executionId}__{workspaceId}__dataset__{datasetName}";
}