using Microsoft.Extensions.Caching.Memory;

namespace Biflow.Executor.Core.Cache;

/// <summary>
/// Represents a caching mechanism for retrieving and storing dataflow IDs in a memory cache.
/// Handles synchronized access across multiple executions and workspaces.
/// </summary>
public class DataflowCache(IMemoryCache cache)
{
    // TODO Handle multiple executions and workspaces at once.
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public async Task<Guid?> GetDataflowIdAsync(
        DataflowClient client,
        Guid executionId,
        Guid workspaceId,
        string dataflowName,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(executionId, workspaceId, dataflowName);
        if (cache.TryGetValue(cacheKey, out Guid cachedDataflowId))
        {
            return cachedDataflowId;
        }
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var dataflows = await client.GetDataflowsAsync(workspaceId, cancellationToken);
            Guid? dataflowId = null;
            foreach (var dataflow in dataflows)
            {
                var key = CacheKey(executionId, workspaceId, dataflow.DataflowName);
                cache.Set(key, dataflow.DataflowId, TimeSpan.FromMinutes(60));
                if (dataflow.DataflowName == dataflowName)
                {
                    dataflowId = dataflow.DataflowId;
                }
            }
            return dataflowId;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private static string CacheKey(Guid executionId, Guid workspaceId, string dataflowName) =>
        $"{executionId}__{workspaceId}__dataflow__{dataflowName}";
}