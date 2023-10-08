namespace Biflow.Ui.Core;

public class FilterIndexer<T>(Dictionary<string, IFilter> filters) where T : IFilter
{
    private readonly Dictionary<string, IFilter> _filters = filters;

    public T this[string index]
    {
        get => (T)_filters[index];
        set => _filters[index] = value;
    }
}
