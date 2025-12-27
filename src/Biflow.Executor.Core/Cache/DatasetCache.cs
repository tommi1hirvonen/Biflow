using Microsoft.Extensions.Caching.Memory;

namespace Biflow.Executor.Core.Cache;

[UsedImplicitly]
internal readonly record struct DatasetCacheConcurrencyKey(Guid ExecutionId, Guid WorkspaceId);

[UsedImplicitly]
internal readonly record struct DatasetCacheKey(Guid ExecutionId, Guid WorkspaceId, string DatasetName);

/// <summary>
/// A cache for Power BI dataset IDs
/// </summary>
/// <param name="cache">The cache used to store dataset IDs</param>
/// <remarks>
/// Inherits from <see cref="AsyncLookupCache{TConcurrencyKey, TCacheKey, TCacheValue, TClient}"/>
/// to manage concurrency in such a way that
/// fetching dataset IDs for a given workspace during the same job execution is serialized.
/// The assumption is that during a single execution,
/// dataset names are unlikely to change and thus caching is safe to do for performance reasons.
/// </remarks>
internal sealed class DatasetCache(IMemoryCache cache)
    : AsyncLookupCache<DatasetCacheConcurrencyKey, DatasetCacheKey, string?, DatasetClient>(cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    /// <inheritdoc />
    protected override async ValueTask PopulateCacheAsync(DatasetClient client, DatasetCacheKey key,
        CancellationToken cancellationToken)
    {
        var datasets = await client.GetDatasetsAsync(key.WorkspaceId, cancellationToken);
        foreach (var dataset in datasets)
        {
            var cacheKey = key with { DatasetName = dataset.DatasetName };
            Cache(cacheKey, dataset.DatasetId, CacheDuration);
        }
    }
}