using System.Linq.Expressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Biflow.Ui.Core;

public static class Extensions
{
    /// <summary>
    /// Extensions to help instruct EF Core ChangeTracker which collection navigation items have changed
    /// when doing a disconnected update.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TKey">Entity key type</typeparam>
    /// <param name="context">DbContext instance whose ChangeTracker is used</param>
    /// <param name="currentItems">Current (old) items</param>
    /// <param name="newItems">New items</param>
    /// <param name="keyFunc">Delegate to get the key from item</param>
    /// <param name="updateMatchingItemValues">call <see cref="PropertyValues.SetValues(object)"/> on matching items</param>
    public static void MergeCollections<T, TKey>(
        this DbContext context,
        ICollection<T> currentItems,
        ICollection<T> newItems,
        Func<T, TKey> keyFunc,
        bool updateMatchingItemValues = true)
        where T : class
    {
        List<T> toRemove = [];
        foreach (var item in currentItems)
        {
            var currentKey = keyFunc(item);
            ArgumentNullException.ThrowIfNull(currentKey);
            var found = newItems.FirstOrDefault(x => currentKey.Equals(keyFunc(x)));
            if (found is null)
            {
                toRemove.Add(item);
            }
            else if (updateMatchingItemValues && !ReferenceEquals(found, item))
            {
                context.Entry(item).CurrentValues.SetValues(found);
            }
        }

        foreach (var item in toRemove)
        {
            currentItems.Remove(item);
            // The following call can be activated if the removed item
            // should be completely deleted from the database.
            // context.Set<T>().Remove(item);
        }

        List<T> toAdd = [];
        foreach (var newItem in newItems)
        {
            var newKey = keyFunc(newItem);
            ArgumentNullException.ThrowIfNull(newKey);
            var found = currentItems.FirstOrDefault(x => newKey.Equals(keyFunc(x)));
            if (found is null)
            {
                toAdd.Add(newItem);
            }
        }

        foreach (var item in toAdd)
        {
            currentItems.Add(item);
        }
    }

    /// <summary>
    /// Executes a query asynchronously and retrieves the results as a list,
    /// using a transaction scope with read-uncommitted isolation level to avoid locking.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <typeparam name="T">The type of elements in the query result.</typeparam>
    /// <returns>A task that represents the asynchronous operation, containing the list of query results.</returns>
    public static async Task<List<T>> ToListWithNoLockAsync<T>(this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.ToListAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Executes a query asynchronously and retrieves the results as an array,
    /// using a transaction scope with read-uncommitted isolation level to avoid locking.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <typeparam name="T">The type of elements in the query result.</typeparam>
    /// <returns>A task that represents the asynchronous operation, containing the array of query results.</returns>
    public static async Task<T[]> ToArrayWithNoLockAsync<T>(this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.ToArrayAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Retrieves the first element of a sequence, or a default value, without acquiring a lock on the database during execution.
    /// </summary>
    /// <param name="query">The queryable sequence to retrieve the element from.</param>
    /// <param name="cancellationToken">Optional cancellation token for managing task cancellation.</param>
    /// <typeparam name="T">The type of the elements in the queryable sequence.</typeparam>
    /// <returns>The first element in that meets the specified condition, or the default value of type T if no such element is found.</returns>
    public static async Task<T?> FirstOrDefaultWithNoLockAsync<T>(this IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.FirstOrDefaultAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies a specified predicate or a default value,
    /// without acquiring a lock on the database during execution.
    /// </summary>
    /// <param name="query">The queryable source to operate on.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A token to monitor for operation cancellation.</param>
    /// <typeparam name="T">The type of elements in the query.</typeparam>
    /// <returns>The first element which satisfies the predicate, or default if no such element is found.</returns>
    public static async Task<T?> FirstOrDefaultWithNoLockAsync<T>(this IQueryable<T> query,
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.FirstOrDefaultAsync(predicate, cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Determines whether any elements exist in the query without acquiring a lock on the database during execution.
    /// </summary>
    /// <param name="query">The queryable data source to evaluate.</param>
    /// /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="cancellationToken">A token to monitor for task cancellation.</param>
    /// <typeparam name="T">Type of the elements in the query.</typeparam>
    /// <returns>True if the query contains any elements; otherwise, false.</returns>
    public static async Task<bool> AnyWithNoLockAsync<T>(this IQueryable<T> query,
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using var scope = CreateReadUncommittedTransaction();
        var result = await query.AnyAsync(predicate, cancellationToken);
        scope.Complete();
        return result;
    }
    
    private static TransactionScope CreateReadUncommittedTransaction() => new(
        TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted },
        TransactionScopeAsyncFlowOption.Enabled);
}
