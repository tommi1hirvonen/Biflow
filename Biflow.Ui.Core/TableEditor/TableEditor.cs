using Dapper;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

public class TableEditorHelper
{
    private readonly string _connectionString;
    private readonly string _schema;
    private readonly string _table;

    private HashSet<string>? PrimaryKeyColumns { get; set; }

    private string? IdentityColumn { get; set; }

    private Dictionary<string, string>? ColumnDbDatatypes { get; set; }

    private LinkedList<RowRecord>? WorkingData { get; set; }

    private int TopRows { get; set; } = 1000;

    public TableEditorHelper(string connectionString, string schema, string table)
    {
        _connectionString = connectionString;
        _schema = schema;
        _table = table;
    }

    public bool IsInitialized => WorkingData is not null;

    public IEnumerable<(string ColumnName, bool IsPrimaryKey)> Columns =>
        ColumnDbDatatypes?.Keys.Select(col => (col, PrimaryKeyColumns?.Contains(col) ?? false))
        ?? Enumerable.Empty<(string, bool)>();

    public IEnumerable<RowRecord> RowRecords =>
        WorkingData?.Where(r => !r.ToBeDeleted) ?? Enumerable.Empty<RowRecord>();

    public FilterSet EmptyFilterSet => new(ColumnDbDatatypes ?? new());

    public async Task LoadDataAsync(int? top = null)
    {
        TopRows = top ?? TopRows;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var primaryKeyColumns = await connection.QueryAsync<string>(
            @"select
                c.[name]
            from sys.index_columns as a
                inner join sys.indexes as b on a.index_id = b.index_id and a.object_id = b.object_id 
                inner join sys.columns as c on a.object_id = c.object_id and a.column_id = c.column_id
                inner join sys.tables as d on a.object_id = d.object_id
                inner join sys.schemas as e on d.schema_id = e.schema_id
            where b.is_primary_key = 1 and d.[name] = @TableName and e.[name] = @SchemaName",
            new { TableName = _table, SchemaName = _schema }
        );

        IdentityColumn = await connection.ExecuteScalarAsync<string?>(
            @"select top 1 a.[name]
            from sys.columns as a
                inner join sys.tables as b on a.object_id = b.object_id
                inner join sys.schemas as c on b.schema_id = c.schema_id
            where a.is_identity = 1 and c.[name] = @SchemaName and b.[name] = @TableName",
            new { TableName = _table, SchemaName = _schema }
        );

        var columnDatatypes = await connection.QueryAsync<(string, string)>(
            @"select
                ColumnName = b.[name],
                DataType = c.[name]
            from sys.tables as a
                inner join sys.columns as b on a.object_id = b.object_id
                inner join sys.types as c on b.user_type_id = c.user_type_id
                inner join sys.schemas as d on a.schema_id = d.schema_id
            where a.[name] = @TableName and d.[name] = @SchemaName",
            new { TableName = _table, SchemaName = _schema }
        );

        PrimaryKeyColumns = primaryKeyColumns.ToHashSet();
        ColumnDbDatatypes = columnDatatypes.ToDictionary(key => key.Item1, value => value.Item2);

        var rows = await connection.QueryAsync($"SELECT TOP {TopRows} * FROM [{_schema}].[{_table}]");
        var originalData = new List<Dictionary<string, object?>>();
        foreach (var row in rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var value in row)
            {
                dict[value.Key] = value.Value;
            }
            originalData.Add(dict);
        }

        var records = originalData.Select(d => new RowRecord(ColumnDbDatatypes, PrimaryKeyColumns, IdentityColumn, d));
        WorkingData = new LinkedList<RowRecord>(records);
    }

    public async Task<(int Inserted, int Updated, int Deleted)> SaveChangesAsync()
    {
        var changes = WorkingData?
            .OrderByDescending(record => record.ToBeDeleted) // handle records to be deleted first
            .Select(record => record.GetChangeSqlCommand(_schema, _table))
            .Where(command => command is not null)
            .Cast<(string Command, DynamicParameters Parameters, DataTableCommandType CommandType)>();

        if (changes is null || !changes.Any())
        {
            return (0, 0, 0);
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var (command, parameters, type) in changes)
            {
                await connection.ExecuteAsync(command, parameters, transaction);
            }
            await transaction.CommitAsync();
            WorkingData = null;
            await LoadDataAsync();
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
        if (WorkingData is null || ColumnDbDatatypes is null || PrimaryKeyColumns is null)
        {
            return;
        }

        var record = new RowRecord(ColumnDbDatatypes, PrimaryKeyColumns, IdentityColumn);
        WorkingData.AddFirst(record);
    }
}
