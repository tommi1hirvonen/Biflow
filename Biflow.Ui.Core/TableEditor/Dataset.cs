using Dapper;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

public class Dataset
{
    private readonly LinkedList<RowRecord> _workingData;

    internal DatasetLoader Loader { get; }

    internal HashSet<string> PrimaryKeyColumns { get; }

    internal string? IdentityColumn { get; }

    internal Dictionary<string, DbDataType> ColumnDbDataTypes { get; }

    internal Dataset(
        DatasetLoader loader,
        HashSet<string> primaryKeyColumns,
        string? identityColumn,
        Dictionary<string, DbDataType> columnDbDataTypes,
        List<Dictionary<string, object?>> data)
    {
        Loader = loader;
        PrimaryKeyColumns = primaryKeyColumns;
        IdentityColumn = identityColumn;
        ColumnDbDataTypes = columnDbDataTypes;
        _workingData = new LinkedList<RowRecord>(data.Select(row => new RowRecord(this, row)));
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

        using var connection = new SqlConnection(Loader.ConnectionString);
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
}
