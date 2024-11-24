namespace Biflow.Ui.TableEditor;

public class ColumnValueIndexer<T>(IDictionary<string, object?> data)
{
    public T? this[string column]
    {
        get => (T?)data[column];
        set => data[column] = value switch
        {
            string { Length: 0 } => null,
            _ => value
        };
    }
}