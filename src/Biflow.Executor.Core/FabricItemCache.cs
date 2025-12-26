using Microsoft.Extensions.Caching.Memory;
using Microsoft.Fabric.Api.Core.Models;

namespace Biflow.Executor.Core;

/// <summary>
/// Represents a caching mechanism for retrieving and storing Fabric item IDs in a memory cache.
/// Handles synchronized access across multiple executions, workspaces, and item types.
/// </summary>
public class FabricItemCache(IMemoryCache cache)
{
    // TODO Handle multiple executions, workspaces and item types at once.
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Guid?> GetItemIdAsync(
        FabricWorkspaceClient client,
        Guid executionId,
        Guid workspaceId,
        FabricItemType itemType,
        string itemName,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(executionId, workspaceId, itemType, itemName);
        if (cache.TryGetValue(cacheKey, out Guid cachedItemId))
        {
            return cachedItemId;
        }
        
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var items = itemType switch
            {
                FabricItemType.DataPipeline => (await client.GetPipelinesAsync(workspaceId, cancellationToken))
                    .Cast<Item>().ToArray(),
                FabricItemType.Notebook => (await client.GetNotebooksAsync(workspaceId, cancellationToken))
                    .Cast<Item>().ToArray(),
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
            Guid? itemId = null;
            foreach (var item in items)
            {
                var key = CacheKey(executionId, workspaceId, itemType, item.DisplayName);
                cache.Set(key, item.Id, TimeSpan.FromMinutes(60));
                if (item.DisplayName == itemName)
                {
                    itemId = item.Id;
                }
            }
            return itemId;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private static string CacheKey(Guid executionId, Guid workspaceId, FabricItemType itemType, string itemName) =>
        $"{executionId}__{workspaceId}__{itemType}__{itemName}";
}