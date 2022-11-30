using System.Data;

namespace Biflow.Ui.Core;

public class Upload
{
    private readonly List<(string ColumnName, string CreateTableDatatype)> _columns;

    public DataTable Data { get; }

    internal Upload(IEnumerable<(string ColumnName, string CreateTableDatatype)> columns, DataTable data)
    {
        _columns = columns.ToList();
        Data = data;
    }
}
