using Biflow.DataAccess.Models;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

public class TableData
{
    private readonly LinkedList<Row> _rows;
    private readonly Column[] _columns;

    internal MasterDataTable MasterDataTable { get; }

    public IEnumerable<Column> Columns => _columns;

    public bool HasChanges { get; internal set; }

    public bool HasMoreRows { get; }

    internal TableData(
        MasterDataTable masterDataTable,
        Column[] columns,
        IEnumerable<IDictionary<string, object?>> data,
        bool hasMoreRows)
    {
        MasterDataTable = masterDataTable;
        _columns = columns;
        _rows = new LinkedList<Row>(data.Select(row => new Row(this, masterDataTable.AllowUpdate, row)));
        HasMoreRows = hasMoreRows;
    }

    public bool IsEditable => _columns.Any(c => c.IsPrimaryKey);

    public FilterSet EmptyFilterSet => new(_columns);

    public IEnumerable<Row> Rows => _rows.Where(r => !r.ToBeDeleted);

    public async Task<(int Inserted, int Updated, int Deleted)> SaveChangesAsync()
    {
        var changes = _rows?
            .OrderByDescending(row => row.ToBeDeleted) // handle records to be deleted first
            .Select(row => row.GetChangeSqlCommand())
            .Where(command => command is not null)
            .Cast<(string Command, DynamicParameters Parameters, DataTableCommandType CommandType)>();

        if (changes is null || !changes.Any())
        {
            return (0, 0, 0);
        }

        using var connection = new SqlConnection(MasterDataTable.Connection.ConnectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var (command, parameters, type) in changes)
            {
                await connection.ExecuteAsync(command, parameters, transaction);
            }
            await transaction.CommitAsync();
            var inserted = changes.Where(c => c.CommandType == DataTableCommandType.Insert).Count();
            var updated = changes.Where(c => c.CommandType == DataTableCommandType.Update).Count();
            var deleted = changes.Where(c => c.CommandType == DataTableCommandType.Delete).Count();
            return (inserted, updated, deleted);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public void AddRow()
    {
        HasChanges = true;
        _rows.AddFirst(new Row(this, true));
    }

    public Stream GetExcelExportStream()
    {
        var workbook = new XLWorkbook(XLEventTracking.Disabled);
        var sheet = workbook.Worksheets.Add("Sheet1");

        var exportColumns = _columns
            .Where(c => !c.IsHidden || c.IsPrimaryKey && MasterDataTable.AllowImport)
            .ToArray();

        var index = 1;
        // Only include columns that are not hidden.
        // If 'allow import' is enabled, include hidden columns if they are part of the primary key.
        foreach (var col in exportColumns)
        {
            sheet.Cell(1, index).SetValue(col.Name);
            index++;
        }
        var rowIndex = 2;
        foreach (var row in _rows)
        {
            var colIndex = 1;
            foreach (var column in exportColumns)
            {
                sheet.Cell(rowIndex, colIndex).Value = row.Values[column.Name];
                colIndex++;
            }
            rowIndex++;
        }
        var firstCell = sheet.Cell(1, 1);
        var lastCell = sheet.Cell(_rows.Count + 1, exportColumns.Length); // Add 1 to row count to account for header row
        var range = sheet.Range(firstCell, lastCell);
        range.CreateTable();
        // Adjust column widths based on only the first 100 rows for much better performance.
        sheet.Columns(1, exportColumns.Length).AdjustToContents(1, 100);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
