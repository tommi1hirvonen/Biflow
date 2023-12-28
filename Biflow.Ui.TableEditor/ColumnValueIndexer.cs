namespace Biflow.Ui.TableEditor;

public class ColumnValueIndexer<T>(IDictionary<string, object?> data)
{
    private readonly IDictionary<string, object?> _data = data;

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