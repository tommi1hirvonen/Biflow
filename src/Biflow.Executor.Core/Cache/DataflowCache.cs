using Microsoft.Extensions.Caching.Memory;

namespace Biflow.Executor.Core.Cache;

[UsedImplicitly]
internal readonly record struct DataflowCacheConcurrencyKey(Guid ExecutionId, Guid WorkspaceId);

[UsedImplicitly]
internal readonly record struct DataflowCacheKey(Guid ExecutionId, Guid WorkspaceId, string DataflowName);

/// <summary>
/// A cache for dataflow IDs
/// </summary>
/// <param name="cache">The cache used to store dataflow IDs</param>
/// <remarks>
/// Inherits from <see cref="AsyncLookupCache{TConcurrencyKey, TCacheKey, TCacheValue, TClient}"/>
/// to manage concurrency in such a way that
/// fetching dataflow IDs for a given workspace during the same job execution is serialized.
/// The assumption is that during a single execution,
/// dataflow names are unlikely to change and thus caching is safe to do for performance reasons.
/// </remarks>
internal sealed class DataflowCache(IMemoryCache cache) :
    AsyncLookupCache<DataflowCacheConcurrencyKey, DataflowCacheKey, Guid?, DataflowClient>(cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

    /// <inheritdoc />
    protected override async ValueTask PopulateCacheAsync(DataflowClient client, DataflowCacheKey key,
        CancellationToken cancellationToken)
    {
        var dataflows = await client.GetDataflowsAsync(key.WorkspaceId, cancellationToken);
        foreach (var dataflow in dataflows)
        {
            var cacheKey = key with { DataflowName = dataflow.DataflowName };
            Cache(cacheKey, dataflow.DataflowId, CacheDuration);
        }
    }
}