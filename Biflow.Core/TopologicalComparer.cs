namespace Biflow.Core;

/// <summary>
/// Compares items topologically based on their dependencies
/// </summary>
/// <typeparam name="TItem">Type of the items to compare</typeparam>
/// <typeparam name="TKey">Type of the key used to uniquely identify each item</typeparam>
public class TopologicalComparer<TItem, TKey> : IComparer<TItem>
    where TKey : notnull
{
    private readonly TKey[] _topologicalList;
    private readonly Func<TItem?, TKey> _keySelector;
    private readonly Func<TItem, IEnumerable<TKey>> _dependenciesSelector;
    private readonly IComparer<TItem>? _whenEqualComparer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="items">Items to compare</param>
    /// <param name="keySelector">Delegate to fetch a unique key for an item</param>
    /// <param name="dependenciesSelector">Delegate to fetch dependencies for an item</param>
    /// <param name="whenEqualComparer">comparer used if two items are topologically equivalent</param>
    /// <exception cref="CyclicDependencyException">If a cyclic dependency is detected and the DFS traversal cannot be completed</exception>
    public TopologicalComparer(
        IEnumerable<TItem> items,
        Func<TItem?, TKey> keySelector,
        Func<TItem, IEnumerable<TKey>> dependenciesSelector,
        IComparer<TItem>? whenEqualComparer = null)
    {
        _keySelector = keySelector;
        _dependenciesSelector = dependenciesSelector;
        _whenEqualComparer = whenEqualComparer;
        var itemsWithDependencies = items.Where(i => dependenciesSelector(i).Any()).ToArray();
        _topologicalList = InTopologicalOrder(itemsWithDependencies)
            .Select(keySelector)
            .ToArray();
    }

    public int Compare(TItem? x, TItem? y)
    {
        var xPos = Array.IndexOf(_topologicalList, _keySelector(x));
        var yPos = Array.IndexOf(_topologicalList, _keySelector(y));
        if (xPos == yPos && _whenEqualComparer is not null)
        {
            return _whenEqualComparer.Compare(x, y);
        }
        return xPos.CompareTo(yPos);
    }

    /// <summary>
    /// Orders an IEnumerable of TItem in a topological order based on their dependencies.
    /// </summary>
    /// <param name="items">IEnumerable of items to be ordered</param>
    /// <returns>IEnumerable of TItem in topological order. InvalidOperationException will be thrown if cyclic dependencies are detected.</returns>
    /// <exception cref="CyclicDependencyException">If a cyclic dependency is detected and the DFS traversal cannot be completed</exception>
    private IEnumerable<TItem> InTopologicalOrder(TItem[] items)
    {
        var stack = new Stack<TItem>();
        var cycles = new List<List<TItem>>();
        var visited = new Dictionary<TKey, VisitState>();
        foreach (var item in items)
        {
            DepthFirstSearch(item, items, [], visited, cycles, stack);
            if (cycles.Count > 0)
            {
                throw new CyclicDependencyException<TItem>(cycles, "Cyclic dependencies detected");
            }
        }
        return stack.Reverse();
    }

    private enum VisitState { NotVisited, Visiting, Visited }

    private void DepthFirstSearch(
        TItem current,
        TItem[] items,
        List<TItem> parents,
        Dictionary<TKey, VisitState> visited,
        List<List<TItem>> cycles,
        Stack<TItem> stack)
    {
        var key = _keySelector(current);
        var state = visited.GetValueOrDefault(key, VisitState.NotVisited);
        switch (state)
        {
            case VisitState.Visited:
                return;
            case VisitState.Visiting:
                var newCycles = parents
                    .Concat([current])
                    .SkipWhile(parent => !EqualityComparer<TKey>.Default.Equals(key, _keySelector(parent)))
                    .ToList();
                cycles.Add(newCycles);
                break;
            case VisitState.NotVisited:
            default:
                visited[key] = VisitState.Visiting;
                parents.Add(current);
                var dependencies = items
                    .Where(item => _dependenciesSelector(current).Any(k => k.Equals(_keySelector(item))))
                    .ToArray();
                foreach (var dependency in dependencies)
                {
                    DepthFirstSearch(dependency, items, parents, visited, cycles, stack);
                }
                parents.RemoveAt(parents.Count - 1);
                visited[key] = VisitState.Visited;
                stack.Push(current);
                break;
        }
    }
}
