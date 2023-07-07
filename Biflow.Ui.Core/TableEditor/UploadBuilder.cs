using Biflow.DataAccess.Models;
using ClosedXML.Excel;
using System.Data;

namespace Biflow.Ui.Core;

public class UploadBuilder
{
    private readonly List<Column> _columns;
    private readonly MasterDataTable _table;

    public IEnumerable<string> Columns => _columns.Select(c => c.Name);

    private UploadBuilder(MasterDataTable table, List<Column> columns)
    {
        _table = table;
        _columns = columns;
    }

    public Upload BuildFromExcelStream(Stream stream)
    {
        using var workbook = new XLWorkbook(stream, XLEventTracking.Disabled);
        var rows = workbook
            .Worksheet(1)
            .Table(0)
            .DataRange
            .Rows();
        var data = new List<IDictionary<string, object?>>();
        foreach (var row in rows)
        {
            var dataRow = new Dictionary<string, object?>();
            foreach (var column in _columns)
            {
                var (col, type) = (column.Name, column.Datatype);
                var cell = row.Field(col);
                if (type == typeof(string))
                {
                    dataRow[col] = cell.GetValue<string?>();
                }
                else if (type == typeof(byte))
                {
                    dataRow[col] = cell.GetValue<byte?>();
                }
                else if (type == typeof(short))
                {
                    dataRow[col] = cell.GetValue<short?>();
                }
                else if (type == typeof(int))
                {
                    dataRow[col] = cell.GetValue<int?>();
                }
                else if (type == typeof(long))
                {
                    dataRow[col] = cell.GetValue<long?>();
                }
                else if (type == typeof(decimal))
                {
                    dataRow[col] = cell.GetValue<decimal?>();
                }
                else if (type == typeof(float))
                {
                    dataRow[col] = cell.GetValue<float?>();
                }
                else if (type == typeof(double))
                {
                    dataRow[col] = cell.GetValue<double?>();
                }
                else if (type == typeof(DateTime))
                {
                    dataRow[col] = cell.GetValue<DateTime?>();
                }
                else if (type == typeof(bool))
                {
                    dataRow[col] = cell.GetValue<bool?>();
                }
                else
                {
                    throw new NotSupportedException($"Unsupported datatype [{column.DbDatatype}] for column {col}");
                }
            }
            data.Add(dataRow);
        }
        return new Upload(_table, _columns, data);
    }

    public static async Task<UploadBuilder> FromTableAsync(MasterDataTable table)
    {
        var columns = (await table.GetColumnsAsync(includeLookups: false)).ToList();
        var notSupportedColumns = columns.Where(c => c.Datatype is null).Select(c => $"c.Name ({c.DbDatatype})");
        if (notSupportedColumns.Any())
        {
            throw new NotSupportedException($"Unsupported database datatypes detected: {string.Join(',', notSupportedColumns)}");
        }
        return new UploadBuilder(table, columns);
    }
}
