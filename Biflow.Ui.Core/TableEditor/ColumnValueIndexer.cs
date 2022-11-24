namespace Biflow.Ui.Core;

public class ColumnValueIndexer<T>
{
    private readonly Dictionary<string, object?> _data;

    public ColumnValueIndexer(Dictionary<string, object?> data)
    {
        _data = data;
    }

    public T? this[string column]
    {
        get => (T?)_data[column];
        set => _data[column] = value switch
        {
            not null and string and { Length: 0 } => null,
            _ => value
        };
    }
}