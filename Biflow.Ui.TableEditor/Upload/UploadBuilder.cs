using ClosedXML.Excel;
using System.Data;

namespace Biflow.Ui.TableEditor;

public class UploadBuilder
{
    private readonly Column[] _columns;
    private readonly MasterDataTable _table;

    public IEnumerable<Column> Columns => _columns;

    private UploadBuilder(MasterDataTable table, Column[] columns)
    {
        _table = table;
        _columns = columns;
    }

    public Upload BuildFromExcelStream(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var rows = workbook
            .Worksheet(1)
            .Table(0)
            .DataRange
            .Rows();
        
        var headerRow = workbook
            .Worksheet(1)
            .Table(0)
            .HeadersRow();
        var headers = new List<string>();
        foreach (var cell in headerRow.Cells())
        {
            var header = cell.GetValue<string>();
            headers.Add(header);
        }

        // Check that all primary key columns are included
        foreach (var pk in _columns.Where(c => c.IsPrimaryKey).Select(c => c.Name))
        {
            if (!headers.Contains(pk))
            {
                throw new PrimaryKeyNotFoundException(pk);
            }
        }

        // Limit uploaded columns to only those found in the Excel file.
        var columns = _columns.Where(c => headers.Contains(c.Name)).ToArray();
        var data = new List<IDictionary<string, object?>>();
        foreach (var row in rows)
        {
            var dataRow = new Dictionary<string, object?>();
            foreach (var column in columns)
            {
                var (col, type, nullable) = (column.Name, column.Datatype, column.IsNullable);
                var cell = row.Field(col);
                if (type == typeof(string))
                {
                    var text = cell.GetValue<string?>(); // If the column is nullable, replace empty string with null.
                    dataRow[col] = string.IsNullOrEmpty(text) && nullable ? null : text;
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
                else if (type == typeof(DateOnly))
                {
                    var dt = cell.GetValue<DateTime?>();
                    dataRow[col] = dt is DateTime notNull ? DateOnly.FromDateTime(notNull) : null;
                }
                else if (type == typeof(TimeOnly))
                {
                    var dt = cell.GetValue<DateTime?>();
                    dataRow[col] = dt is DateTime notNull ? TimeOnly.FromDateTime(notNull) : null;
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
        return new Upload(_table, columns, data);
    }

    public static async Task<UploadBuilder> FromTableAsync(MasterDataTable table)
    {
        var columns = (await table.GetColumnsAsync(includeLookups: false)).ToArray();
        var notSupportedColumns = columns.Where(c => c.Datatype is null).Select(c => $"c.Name ({c.DbDatatype})");
        if (notSupportedColumns.Any())
        {
            throw new NotSupportedException($"Unsupported database datatypes detected: {string.Join(',', notSupportedColumns)}");
        }
        return new UploadBuilder(table, columns);
    }
}
