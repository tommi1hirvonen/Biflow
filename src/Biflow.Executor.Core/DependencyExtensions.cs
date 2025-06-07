namespace Biflow.Executor.Core;

internal static class DependencyExtensions
{
    private enum VisitState
    {
        NotVisited,
        Visiting,
        Visited
    }

    private static TValue? ValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue)
    {
        return dictionary.TryGetValue(key, out var value)
            ? value
            : defaultValue;
    }

    private static void DepthFirstSearch<T>(T node, Func<T, IEnumerable<T>> lookup, List<T> parents, Dictionary<T, VisitState> visited, List<List<T>> cycles) where T : notnull
    {
        var state = visited.ValueOrDefault(node, VisitState.NotVisited);
        switch (state)
        {
            case VisitState.Visited:
                return;
            case VisitState.Visiting:
                // Do not report nodes not included in the cycle.
                cycles.Add(
                    parents.Concat([node])
                        .SkipWhile(parent => !EqualityComparer<T>.Default.Equals(parent, node))
                        .ToList());
                break;
            case VisitState.NotVisited:
            default:
                visited[node] = VisitState.Visiting;
                parents.Add(node);
                foreach (var child in lookup(node))
                {
                    DepthFirstSearch(child, lookup, parents, visited, cycles);
                }
                parents.RemoveAt(parents.Count - 1);
                visited[node] = VisitState.Visited;
                break;
        }
    }

    private static List<List<T>> FindCycles<T>(this IEnumerable<T> nodes, Func<T, IEnumerable<T>> edges)
        where T : notnull
    {
        var cycles = new List<List<T>>();
        var visited = new Dictionary<T, VisitState>();
        foreach (var node in nodes)
        {
            DepthFirstSearch(node, edges, [], visited, cycles);
        }
        return cycles;
    }

    public static List<List<T>> FindCycles<T, TValueList>(this IDictionary<T, TValueList> listDictionary)
        where TValueList : class, IEnumerable<T>
        where T : notnull
    {
        return listDictionary.Keys.FindCycles(key => listDictionary.ValueOrDefault(key, null) ?? Enumerable.Empty<T>());
    }
}
