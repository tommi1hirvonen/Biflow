using System.Collections.Concurrent;

namespace Biflow.Executor.Core.Cache;

/// <summary>
/// Provides an asynchronous, keyed mutual-exclusion mechanism.
/// </summary>
/// <typeparam name="TKey">
/// The key type used to partition locks. Each distinct key has its own
/// independent asynchronous lock.
/// </typeparam>
/// <remarks>
/// <para>
/// This type implements a safe "keyed async lock" pattern suitable for
/// synchronizing asynchronous operations without blocking threads.
/// </para>
/// <para>
/// For each unique key, a single <see cref="SemaphoreSlim"/> is created and
/// shared by all callers requesting the same key. The semaphore is reference-counted
/// and removed automatically when no operations are using it.
/// </para>
/// <para>
/// This implementation avoids common concurrency hazards:
/// <list type="bullet">
/// <item><description>
/// ABA races when removing semaphores from a dictionary
/// </description></item>
/// <item><description>
/// Concurrent creation of multiple semaphores for the same key
/// </description></item>
/// <item><description>
/// Leaking or prematurely disposing synchronization primitives
/// </description></item>
/// </list>
/// </para>
/// <para>
/// The lock is acquired asynchronously and released via the returned
/// <see cref="IDisposable"/>. Callers <b>must</b> dispose the returned
/// instance to release the lock.
/// </para>
/// </remarks>
internal sealed class AsyncKeyedLock<TKey>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Entry> _entries = new();

    /// <summary>
    /// Asynchronously acquires the lock associated with the specified key.
    /// </summary>
    /// <param name="key">
    /// The key identifying which lock to acquire.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel waiting for the lock.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that completes when the lock has been
    /// acquired. The returned <see cref="IDisposable"/> must be disposed to
    /// release the lock.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown if <paramref name="cancellationToken"/> is canceled while waiting
    /// for the lock.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If multiple callers request the same key concurrently, they are
    /// serialized such that only one caller holds the lock for that key
    /// at a time.
    /// </para>
    /// <para>
    /// Locks for different keys are independent and do not block one another.
    /// </para>
    /// </remarks>
    public async ValueTask<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default)
    {
        var entry = _entries.GetOrAdd(key, _ => new Entry());

        // Increment reference count before waiting to ensure the entry
        // cannot be removed while this caller is pending.
        Interlocked.Increment(ref entry.RefCount);

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Releaser(this, key, entry);
        }
        catch
        {
            // If waiting fails (e.g., due to cancellation), release the
            // reference count acquired above.
            ReleaseRef(key, entry);
            throw;
        }
    }

    /// <summary>
    /// Releases a reference to a lock entry and removes it from the dictionary
    /// when no references remain.
    /// </summary>
    private void ReleaseRef(TKey key, Entry entry)
    {
        if (Interlocked.Decrement(ref entry.RefCount) == 0)
        {
            // Remove only if the dictionary still maps this key to
            // the same entry instance, avoiding ABA races.
            _entries.TryRemove(new KeyValuePair<TKey, Entry>(key, entry));
        }
    }

    /// <summary>
    /// Represents a single keyed lock entry.
    /// </summary>
    private sealed class Entry
    {
        /// <summary>
        /// The semaphore providing mutual exclusion for this key.
        /// </summary>
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        
        /// <summary>
        /// The number of active or pending users of this lock entry.
        /// </summary>
        public int RefCount;
    }

    /// <summary>
    /// Releases a previously acquired keyed lock.
    /// </summary>
    private sealed class Releaser(AsyncKeyedLock<TKey> owner, TKey key, Entry entry) : IDisposable
    {
        private int _disposed;

        /// <summary>
        /// Releases the lock and updates the internal reference count.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            entry.Semaphore.Release();
            owner.ReleaseRef(key, entry);
        }
    }
}
