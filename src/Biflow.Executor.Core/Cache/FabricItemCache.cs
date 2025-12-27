using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Fabric.Api.Core.Models;

namespace Biflow.Executor.Core.Cache;

[UsedImplicitly]
internal readonly record struct FabricItemCacheConcurrencyKey(
    Guid ExecutionId,
    Guid WorkspaceId,
    FabricItemType ItemType);

[UsedImplicitly]
internal readonly record struct FabricItemCacheKey(
    Guid ExecutionId,
    Guid WorkspaceId,
    FabricItemType ItemType,
    string ItemName);

/// <summary>
/// A cache for Fabric item IDs
/// </summary>
/// <param name="cache">The cache used to store item IDs</param>
/// <remarks>
/// Inherits from <see cref="AsyncLookupCache{TConcurrencyKey, TCacheKey, TCacheValue, TClient}"/>
/// to manage concurrency in such a way that
/// fetching item IDs for a given workspace during the same job execution is serialized.
/// The assumption is that during a single execution,
/// item names are unlikely to change and thus caching is safe to do for performance reasons.
/// </remarks>
internal sealed class FabricItemCache(IMemoryCache cache, ILogger<FabricItemCache> logger)
    : AsyncLookupCache<FabricItemCacheConcurrencyKey, FabricItemCacheKey, Guid?, FabricWorkspaceClient>(cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    /// <inheritdoc />
    protected override async ValueTask PopulateCacheAsync(FabricWorkspaceClient client, FabricItemCacheKey key,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Populating cache for workspace id {WorkspaceId} and item type {ItemType}",
            key.WorkspaceId, key.ItemType);
        IEnumerable<Item> items = key.ItemType switch
        {
            FabricItemType.DataPipeline => await client.GetPipelinesAsync(key.WorkspaceId, cancellationToken),
            FabricItemType.Notebook => await client.GetNotebooksAsync(key.WorkspaceId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(message: $"Unrecognized ItemType value: {key.ItemType}",
                innerException: null)
        };

        // Populate cache for all items in the enumeration
        foreach (var item in items)
        {
            var cacheKey = key with { ItemName = item.DisplayName };
            Cache(cacheKey, item.Id, CacheDuration);
        }
    }
}