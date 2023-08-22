using Biflow.DataAccess.Models;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

public class TableData
{
    private readonly LinkedList<Row> _rows;

    internal MasterDataTable MasterDataTable { get; }

    public HashSet<Column> Columns { get; }

    public bool HasChanges { get; internal set; }

    public bool HasMoreRows { get; }

    internal TableData(
        MasterDataTable masterDataTable,
        HashSet<Column> columns,
        IEnumerable<IDictionary<string, object?>> data,
        bool hasMoreRows)
    {
        MasterDataTable = masterDataTable;
        Columns = columns;
        _rows = new LinkedList<Row>(data.Select(row => new Row(this, masterDataTable.AllowUpdate, row)));
        HasMoreRows = hasMoreRows;
    }

    public bool IsEditable => Columns.Any(c => c.IsPrimaryKey);

    public FilterSet EmptyFilterSet => new(Columns);

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
        for (int i = 0; i < Columns.Count; i++)
        {
            sheet.Cell(1, i + 1).SetValue(Columns.ElementAt(i).Name);
        }
        var rowIndex = 2;
        foreach (var row in _rows)
        {
            var colIndex = 1;
            foreach (var column in Columns)
            {
                sheet.Cell(rowIndex, colIndex).Value = row.Values[column.Name];
                colIndex++;
            }
            rowIndex++;
        }
        var firstCell = sheet.Cell(1, 1);
        var lastCell = sheet.Cell(_rows.Count + 1, Columns.Count); // Add 1 to row count to account for header row
        var range = sheet.Range(firstCell, lastCell);
        range.CreateTable();
        // Adjust column widths based on only the first 100 rows for much better performance.
        sheet.Columns(1, Columns.Count).AdjustToContents(1, 100);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
