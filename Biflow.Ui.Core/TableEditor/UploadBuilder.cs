using Biflow.DataAccess.Models;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

public class UploadBuilder
{
    private readonly List<(string ColumnName, Type Datatype, string CreateDatatype)> _columns;

    public IEnumerable<string> Columns => _columns.Select(c => c.ColumnName);

    private UploadBuilder(List<(string ColumnName, Type Datatype, string CreateDatatype)> columns)
    {
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
        foreach (var (name, type, _) in _columns)
        {
            data.Columns.Add(name, type);
        }
        foreach (var row in rows)
        {
            var dataRow = data.NewRow();
            foreach (var (col, type, _) in _columns)
            {
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
                else
                {
                    throw new NotSupportedException($"Unsupported datatype {type} for column {col}");
                }
            }
            data.Rows.Add(dataRow);
        }
        return new Upload(_columns.Select(c => (c.ColumnName, c.CreateDatatype)), data);
    }

    public static async Task<UploadBuilder> FromTableAsync(DataTable table)
    {
        using var connection = new SqlConnection(table.Connection.ConnectionString);
        await connection.OpenAsync();
        var rows = await table.GetColumnDatatypesAsync(connection);
        var columns = rows.Select(row =>
        {
            var datatype = TableEditorExtensions.DatatypeMapping.GetValueOrDefault(row.Datatype)
                ?? throw new NotSupportedException($"Database datatype {row.Datatype} is not supported");
            return (row.Name, datatype, row.CreateDatatype);
            
        }).ToList();
        return new UploadBuilder(columns);
    }
}
