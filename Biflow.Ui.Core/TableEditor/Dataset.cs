using Biflow.DataAccess.Models;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

public class Dataset
{
    private readonly LinkedList<RowRecord> _workingData;

    internal MasterDataTable MasterDataTable { get; }

    public HashSet<Column> Columns { get; }

    internal Dataset(
        MasterDataTable masterDataTable,
        HashSet<Column> columns,
        IEnumerable<IDictionary<string, object?>> data)
    {
        MasterDataTable = masterDataTable;
        Columns = columns;
        _workingData = new LinkedList<RowRecord>(data.Select(row => new RowRecord(this, row)));
    }

    public bool IsEditable => Columns.Any(c => c.IsPrimaryKey);

    public FilterSet EmptyFilterSet => new(Columns);

    public IEnumerable<RowRecord> RowRecords => _workingData.Where(r => !r.ToBeDeleted);

    public async Task<(int Inserted, int Updated, int Deleted)> SaveChangesAsync()
    {
        var changes = _workingData?
            .OrderByDescending(record => record.ToBeDeleted) // handle records to be deleted first
            .Select(record => record.GetChangeSqlCommand())
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

    public void AddRecord()
    {
        var record = new RowRecord(this);
        _workingData.AddFirst(record);
    }

    public Stream GetExcelExportStream()
    {
        var data = new System.Data.DataTable();
        foreach (var column in Columns)
        {
            data.Columns.Add(column.Name);
        }
        foreach (var rowRecod in _workingData)
        {
            var row = data.NewRow();
            var values = rowRecod.WorkingValues;
            foreach (var (column, value) in values)
            {
                row[column] = value;
            }
            data.Rows.Add(row);
        }
        var workbook = new XLWorkbook(XLEventTracking.Disabled);
        var sheet = workbook.Worksheets.Add(data, "Sheet1");
        // Adjust column widths based on only the first 100 rows for much better performance.
        sheet.Columns(1, Columns.Count).AdjustToContents(1, 100);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
