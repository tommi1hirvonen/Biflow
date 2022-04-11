using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;

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

    public async Task LoadDataAsync(int? top = null, FilterSet? filters = null)
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

        var cmdBuilder = new StringBuilder();
        var parameters = new DynamicParameters();

        cmdBuilder.Append("SELECT TOP ").Append(TopRows).Append(" * FROM [").Append(_schema).Append("].[").Append(_table).Append(']');

        if (filters?.Filters.Any(f => f.Value.Enabled1) ?? false)
        {
            cmdBuilder.Append(" WHERE");
            var index = 1;
            foreach (var (column, filter) in filters.Filters.Where(f => f.Value.Enabled1))
            {
                if (index > 1)
                {
                    cmdBuilder.Append(" AND ");
                }
                cmdBuilder.Append(" [").Append(column).Append("] ");
                if (filter.Operator1 is NumberFilterOperator nfo)
                {
                    var operatorText = nfo switch
                    {
                        NumberFilterOperator.Equals => " = ",
                        NumberFilterOperator.DoesNotEqual => " <> ",
                        NumberFilterOperator.GreaterThan => " > ",
                        NumberFilterOperator.GreaterThanOrEqual => " >= ",
                        NumberFilterOperator.LessThan => " < ",
                        NumberFilterOperator.LessThanOrEqual => " <= ",
                        NumberFilterOperator.IsBlank => " IS NULL",
                        NumberFilterOperator.IsNotBlank => " IS NOT NULL",
                        _ => throw new ArgumentException($"Unsupported NumberFilterOperator value {nfo}")
                    };
                    if (nfo == NumberFilterOperator.IsBlank || nfo == NumberFilterOperator.IsNotBlank)
                    {
                        cmdBuilder.Append(operatorText);
                    }
                    else
                    {
                        cmdBuilder.Append(operatorText).Append("@Parameter_").Append(index);
                        parameters.Add($"Parameter_{index}", filter.FilterValue1);
                    }
                }
                else if (filter.Operator1 is TextFilterOperator tfo)
                {
                    var operatorText = tfo switch
                    {
                        TextFilterOperator.Equals => " = ",
                        TextFilterOperator.DoesNotEqual => " <> ",
                        TextFilterOperator.Contains => " LIKE ",
                        TextFilterOperator.DoesNotContain => " NOT LIKE ",
                        TextFilterOperator.StartsWith => " LIKE ",
                        TextFilterOperator.DoesNotStartWith => " NOT LIKE ",
                        TextFilterOperator.EndsWith => " LIKE ",
                        TextFilterOperator.DoesNotEndWith => " NOT LIKE ",
                        TextFilterOperator.GreaterThan => " > ",
                        TextFilterOperator.GreaterThanOrEqual => " >= ",
                        TextFilterOperator.LessThan => " < ",
                        TextFilterOperator.LessThanOrEqual => " <= ",
                        TextFilterOperator.IsBlank => " IS NULL",
                        TextFilterOperator.IsNotBlank => " IS NOT NULL",
                        _ => throw new ArgumentException($"Unsupported TextFilterOperator value {tfo}")
                    };
                    static string encodeForLike(string term) => term.Replace("[", "[[]").Replace("%", "[%]");
                    var value = filter.FilterValue1;
                    if (tfo == TextFilterOperator.IsBlank || tfo == TextFilterOperator.IsNotBlank)
                    {
                        cmdBuilder.Append(operatorText);
                    }
                    
                    if (tfo == TextFilterOperator.Contains || tfo == TextFilterOperator.DoesNotContain)
                    {
                        value = $"%{encodeForLike(value.ToString() ?? "")}%";
                    }
                    else if (tfo == TextFilterOperator.StartsWith || tfo == TextFilterOperator.DoesNotStartWith)
                    {
                        value = $"{encodeForLike(value.ToString() ?? "")}%";
                    }
                    else if (tfo == TextFilterOperator.EndsWith || tfo == TextFilterOperator.DoesNotEndWith)
                    {
                        value = $"%{encodeForLike(value.ToString() ?? "")}";
                    }

                    if (tfo != TextFilterOperator.IsBlank && tfo != TextFilterOperator.IsNotBlank)
                    {
                        cmdBuilder.Append(operatorText).Append("@Parameter_").Append(index);
                        parameters.Add($"Parameter_{index}", value);
                    }
                }
                else if (filter.Operator1 is BooleanFilterOperator bfo)
                {
                    var operatorText = bfo switch
                    {
                        BooleanFilterOperator.Equals => " = ",
                        _ => throw new ArgumentException($"Unsupported BooleanFilterOperator value {bfo}")
                    };
                    cmdBuilder.Append(operatorText).Append("@Parameter_").Append(index);
                    parameters.Add($"Parameter_{index}", filter.FilterValue1);
                }
                else
                {
                    throw new ArgumentException($"Unsupported filter operator type {filter.Operator1.GetType()}");
                }
                index++;
            }
        }

        var cmd = cmdBuilder.ToString();
        var rows = await connection.QueryAsync(cmd, parameters);
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
