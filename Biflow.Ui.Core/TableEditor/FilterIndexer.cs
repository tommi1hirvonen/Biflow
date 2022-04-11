namespace Biflow.Ui.Core;

public class FilterIndexer<T> where T : IFilter
{
    private readonly Dictionary<string, IFilter> _filters;

    public FilterIndexer(Dictionary<string, IFilter> filters)
    {
        _filters = filters;
    }

    public T this[string index]
    {
        get => (T)_filters[index];
        set => _filters[index] = value;
    }
}
