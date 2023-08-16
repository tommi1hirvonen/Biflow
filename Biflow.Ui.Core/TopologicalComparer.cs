namespace Biflow.Ui.Core;

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="items">Items to compare</param>
    /// <param name="keySelector">Delegate to fetch a unique key for an item</param>
    /// <param name="dependenciesSelector">Delegate to fetch dependencies for an item</param>
    public TopologicalComparer(IEnumerable<TItem> items, Func<TItem?, TKey> keySelector, Func<TItem, IEnumerable<TKey>> dependenciesSelector)
    {
        _keySelector = keySelector;
        _dependenciesSelector = dependenciesSelector;
        _topologicalList = InTopologicalOrder(items.ToArray())
            .Select(keySelector)
            .ToArray();
    }

    public int Compare(TItem? x, TItem? y)
    {
        int xPos = Array.IndexOf(_topologicalList, _keySelector(x));
        int yPos = Array.IndexOf(_topologicalList, _keySelector(y));
        return xPos.CompareTo(yPos);
    }

    /// <summary>
    /// Orders an IEnumerable of TItem in a topological order based on their dependencies.
    /// </summary>
    /// <param name="steps">IEnumerable of items to be ordered</param>
    /// <returns>IEnumerable of TItem in topological order. InvalidOperationException will be thrown if cyclic dependencies are detected.</returns>
    private IEnumerable<TItem> InTopologicalOrder(TItem[] items)
    {
        var stack = new Stack<TItem>();
        var visited = new Dictionary<TKey, VisitState>();
        foreach (var item in items)
        {
            if (!DepthFirstSearch(item, items, visited, stack))
            {
                throw new InvalidOperationException("Cyclic dependencies detected");
            }
        }
        return stack.Reverse();
    }

    private enum VisitState { NotVisited, Visiting, Visited }

    private bool DepthFirstSearch(TItem current, TItem[] items, Dictionary<TKey, VisitState> visited, Stack<TItem> stack)
    {
        var state = VisitState.NotVisited;
        var key = _keySelector(current);
        visited.TryGetValue(key, out state);
        if (state != VisitState.NotVisited)
        {
            return state == VisitState.Visited; // returns false if already visiting => cycles
        }
        visited[key] = VisitState.Visiting;
        var dependencies = items
            .Where(item => _dependenciesSelector(current).Any(key => key.Equals(_keySelector(item))))
            .ToArray();
        var result = dependencies.Aggregate(true, (accumulator, item) => accumulator && DepthFirstSearch(item, items, visited, stack));
        visited[key] = VisitState.Visited;
        stack.Push(current);
        return result;
    }
}
