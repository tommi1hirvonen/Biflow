namespace Biflow.Executor.Core.Cache;

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides a base class for implementing asynchronous, keyed lookup caches
/// that resolve values via potentially expensive or remote operations.
/// </summary>
/// <typeparam name="TConcurrencyKey">
/// The type of the key used to manage concurrency. Each distinct key has
/// its own independent concurrency lock.
/// </typeparam>
/// <typeparam name="TCacheKey">
/// The type of the key used to identify cached values.
/// </typeparam>
/// <typeparam name="TCacheValue">
/// The type of value stored in the cache. Derived classes may populate one
/// or more cache entries per lookup.
/// </typeparam>
/// <typeparam name="TClient">
/// The type of the client that can be used to fetch items for populating the cache when a cache-miss occurs.
/// </typeparam>
/// <remarks>
/// <para>
/// <see cref="AsyncLookupCache{TClient, TConcurrencyKey, TCacheKey, TCacheValue}"/> provides a pattern for
/// asynchronous cache lookups that may involve remote or expensive
/// operations. It ensures that for each key, at most one asynchronous
/// population operation occurs concurrently.
/// </para>
/// <para>
/// This base class **does not directly write cache entries**. Instead, derived
/// classes implement <see cref="PopulateCacheAsync(TClient, TCacheKey, CancellationToken)"/>,
/// which is responsible for storing the value(s) in the cache. This design
/// supports scenarios where a single lookup produces multiple cache entries
/// (fan-out caching).
/// </para>
/// <para>
/// Derived classes are expected to ensure that after
/// <see cref="PopulateCacheAsync(TClient, TCacheKey, CancellationToken)"/> completes, the
/// requested key is present in the cache (or intentionally absent if a value
/// cannot be resolved).
/// </para>
/// </remarks>
internal abstract class AsyncLookupCache<TConcurrencyKey, TCacheKey, TCacheValue, TClient>
    where TConcurrencyKey : notnull
    where TCacheKey : notnull
{
    /// <summary>
    /// A keyed asynchronous lock used to serialize cache population per key.
    /// </summary>
    private readonly AsyncKeyedLock<TConcurrencyKey> _keyedLock = new();

    /// <summary>
    /// The underlying memory cache used to store resolved values.
    /// </summary>
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLookupCache{TConcurrencyKey, TCacheKey, TCacheValue, TClient}"/> class.
    /// </summary>
    /// <param name="cache">
    /// The memory cache instance used to store resolved values.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="cache"/> is <see langword="null"/>.
    /// </exception>
    protected AsyncLookupCache(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }
    
    /// <summary>
    /// Asynchronously retrieves the value associated with the specified key,
    /// using the cache if possible.
    /// </summary>
    /// <param name="client">
    /// The client which can be used to fetch items for populating the cache when a cache-miss occurs.
    /// </param>
    /// <param name="concurrencyKey">
    /// The key for asynchronously locking and serializing cache population.
    /// </param>
    /// <param name="cacheKey">
    /// The key whose associated value should be retrieved.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that completes with the cached or newly resolved value. If the
    /// value cannot be resolved and is not populated in the cache, the result
    /// will be the default for <typeparamref name="TCacheValue"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown if <paramref name="cancellationToken"/> is canceled while
    /// waiting for the lock or during the lookup.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method first attempts a fast, lock-free cache lookup. If the value
    /// is not present, a keyed asynchronous lock is acquired to ensure that
    /// only one caller populates the cache for the specified key at a time.
    /// </para>
    /// <para>
    /// After acquiring the lock, the cache is checked again to avoid redundant
    /// lookups if another caller populated the cache while waiting.
    /// </para>
    /// <para>
    /// The actual population of cache entries is delegated to
    /// <see cref="PopulateCacheAsync(TClient, TCacheKey, CancellationToken)"/>, which may
    /// populate multiple keys if needed.
    /// </para>
    /// </remarks>
    public async Task<TCacheValue?> GetAsync(TClient client, TConcurrencyKey concurrencyKey, TCacheKey cacheKey,
        CancellationToken cancellationToken = default)
    {
        // Fast path: lock-free cache lookup
        if (_cache.TryGetValue(cacheKey, out TCacheValue? value))
        {
            return value;
        }

        using (await _keyedLock.LockAsync(concurrencyKey, cancellationToken))
        {
            // Double-check cache after acquiring the lock
            if (_cache.TryGetValue(cacheKey, out value))
            {
                return value;
            }

            // Delegate *all* cache population to the derived class
            await PopulateCacheAsync(client, cacheKey, cancellationToken);

            // Value must now be present (or intentionally absent)
            _cache.TryGetValue(cacheKey, out value);
            return value;
        }
    }

    protected void Cache(TCacheKey key, TCacheValue value, TimeSpan absoluteExpirationRelativeToNow) =>
        _cache.Set(key, value, absoluteExpirationRelativeToNow);

    /// <summary>
    /// Populates one or more cache entries for the specified key.
    /// </summary>
    /// <param name="client">
    /// The client which can be used to fetch items for populating the cache.
    /// </param>
    /// <param name="key">
    /// The key whose value(s) should be populated in the cache.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the population operation.
    /// </param>
    /// <returns>
    /// A task that completes when cache population is finished.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Derived classes implement this method to perform the actual resolution
    /// of values (e.g., via remote service calls) and store the results in
    /// <see cref="_cache"/>.
    /// </para>
    /// <para>
    /// The requested <paramref name="key"/> should be present in the cache when
    /// this method completes (unless the value cannot be resolved).
    /// </para>
    /// <para>
    /// This method is guaranteed to be called by at most one concurrent caller
    /// per key.
    /// </para>
    /// </remarks>
    protected abstract ValueTask PopulateCacheAsync(TClient client, TCacheKey key, CancellationToken cancellationToken);
}
