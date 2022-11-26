using Biflow.DataAccess.Models;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

public class Dataset
{
    private readonly LinkedList<RowRecord> _workingData;
    
    internal Dictionary<string, IEnumerable<(object? Value, object? DisplayValue)>> LookupData { get; }

    internal DataTable DataTable { get; }

    internal HashSet<string> PrimaryKeyColumns { get; }

    internal string? IdentityColumn { get; }

    internal Dictionary<string, DbDataType> ColumnDbDataTypes { get; }

    internal Dataset(
        DataTable dataTable,
        HashSet<string> primaryKeyColumns,
        string? identityColumn,
        Dictionary<string, DbDataType> columnDbDataTypes,
        IEnumerable<IDictionary<string, object?>> data,
        Dictionary<string, IEnumerable<(object? Value, object? DisplayValue)>> lookupData)
    {
        DataTable = dataTable;
        PrimaryKeyColumns = primaryKeyColumns;
        IdentityColumn = identityColumn;
        ColumnDbDataTypes = columnDbDataTypes;
        _workingData = new LinkedList<RowRecord>(data.Select(row => new RowRecord(this, row)));
        LookupData = lookupData;
    }

    public bool IsEditable => PrimaryKeyColumns.Any();

    public FilterSet EmptyFilterSet => new(ColumnDbDataTypes);

    public IEnumerable<(string ColumnName, DbDataType DataType, bool IsPrimaryKey)> Columns =>
        ColumnDbDataTypes.Keys.Select(col => (col, ColumnDbDataTypes[col], PrimaryKeyColumns.Contains(col)));

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

        using var connection = new SqlConnection(DataTable.Connection.ConnectionString);
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
        var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Export");
        foreach (var (column, index) in ColumnDbDataTypes.Keys.Select((c, i) => (c, i)))
        {
            sheet.Cell(1, index + 1).SetValue(column).Style.Font.Bold = true;
        }
        foreach (var (rowRecod, rowIndex) in _workingData.Select((r, i) => (r, i)))
        {
            var values = rowRecod.WorkingValues.Values.Select((v, i) => (v, i));
            foreach (var (value, columnIndex) in values)
            {
                sheet.Cell(rowIndex + 2, columnIndex + 1).Value = value;
            }
        }
        // Adjust column widths based on only the first 100 rows for much better performance.
        sheet.Columns(1, ColumnDbDataTypes.Keys.Count).AdjustToContents(1, 100);
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
