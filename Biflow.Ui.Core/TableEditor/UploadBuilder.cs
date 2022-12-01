using Biflow.DataAccess.Models;
using ClosedXML.Excel;

namespace Biflow.Ui.Core;

public class UploadBuilder
{
    private readonly List<Column> _columns;
    private readonly DataTable _table;

    public IEnumerable<string> Columns => _columns.Select(c => c.Name);

    private UploadBuilder(DataTable table, List<Column> columns)
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
        var data = new System.Data.DataTable();
        foreach (var column in _columns)
        {
            ArgumentNullException.ThrowIfNull(column.Datatype);
            data.Columns.Add(column.Name, column.Datatype);
        }
        foreach (var row in rows)
        {
            var dataRow = data.NewRow();
            foreach (var column in _columns)
            {
                var (col, type) = (column.Name, column.Datatype);
                var cell = row.Field(col);
                if (type == typeof(string))
                {
                    dataRow[col] = cell.GetString();
                }
                else if (type == typeof(byte))
                {
                    dataRow[col] = cell.GetValue<byte>();
                }
                else if (type == typeof(short))
                {
                    dataRow[col] = cell.GetValue<short>();
                }
                else if (type == typeof(int))
                {
                    dataRow[col] = cell.GetValue<int>();
                }
                else if (type == typeof(long))
                {
                    dataRow[col] = cell.GetValue<long>();
                }
                else if (type == typeof(decimal))
                {
                    dataRow[col] = cell.GetValue<decimal>();
                }
                else if (type == typeof(float))
                {
                    dataRow[col] = cell.GetValue<float>();
                }
                else if (type == typeof(double))
                {
                    dataRow[col] = cell.GetDouble();
                }
                else if (type == typeof(int))
                {
                    dataRow[col] = cell.GetDateTime();
                }
                else if (type == typeof(bool))
                {
                    dataRow[col] = cell.GetBoolean();
                }
                else if (type == typeof(DateTime))
                {
                    dataRow[col] = cell.GetDateTime();
                }
                else
                {
                    throw new NotSupportedException($"Unsupported datatype {type} for column {col}");
                }
            }
            data.Rows.Add(dataRow);
        }
        return new Upload(_table, _columns, data);
    }

    public static async Task<UploadBuilder> FromTableAsync(DataTable table)
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
