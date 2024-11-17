namespace Biflow.Ui.TableEditor;

public class FilterIndexer<T>(Dictionary<string, IFilter> filters) where T : IFilter
{
    public T this[string index]
    {
        get => (T)filters[index];
        set => filters[index] = value;
    }
}
