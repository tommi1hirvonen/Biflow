using Dapper;
using System.Text;

namespace Biflow.Ui.Core;

public class RowRecord
{
    private readonly Dictionary<string, string> _columnDbDatatypes;
    private readonly HashSet<string> _primaryKeyColumns;
    private readonly string? _identityColumn;

    private Dictionary<string, object?>? OriginalValues { get; }

    public Dictionary<string, object?> WorkingValues { get; }

    public ColumnValueIndexer<byte?> ByteIndexer { get; }
    public ColumnValueIndexer<short?> ShortIndexer { get; }
    public ColumnValueIndexer<int?> IntIndexer { get; }
    public ColumnValueIndexer<long?> LongIndexer { get; }
    public ColumnValueIndexer<decimal?> DecimalIndexer { get; }
    public ColumnValueIndexer<double?> DoubleIndexer { get; }
    public ColumnValueIndexer<float?> FloatIndexer { get; }
    public ColumnValueIndexer<string?> StringIndexer { get; }
    public ColumnValueIndexer<bool?> BooleanIndexer { get; }
    public ColumnValueIndexer<DateTime?> DateTimeIndexer { get; }

    public bool ToBeDeleted { get; set; }

    public RowRecord(
        Dictionary<string, string> columnDbDatatypes,
        HashSet<string> primaryKeyColumns,
        string? identityColumn,
        Dictionary<string, object?>? originalValues = null)
    {
        _columnDbDatatypes = columnDbDatatypes;
        _primaryKeyColumns = primaryKeyColumns;
        _identityColumn = identityColumn;
        OriginalValues = originalValues;
        WorkingValues = OriginalValues is not null ? new(OriginalValues) : new();
        
        ByteIndexer = new(WorkingValues);
        ShortIndexer = new(WorkingValues);
        IntIndexer = new(WorkingValues);
        LongIndexer = new(WorkingValues);
        DecimalIndexer = new(WorkingValues);
        DoubleIndexer = new(WorkingValues);
        FloatIndexer = new(WorkingValues);
        StringIndexer = new(WorkingValues);
        BooleanIndexer = new(WorkingValues);
        DateTimeIndexer = new(WorkingValues);

        if (OriginalValues is null)
        {
            foreach (var columnInfo in _columnDbDatatypes)
            {
                var column = columnInfo.Key;
                var dbDatatype = columnInfo.Value;
                if (column != _identityColumn && DataTypeMapping.TryGetValue(dbDatatype, out var datatype))
                {
                    if (datatype == typeof(byte))
                        ByteIndexer[column] = 0;
                    else if (datatype == typeof(short))
                        ShortIndexer[column] = 0;
                    else if (datatype == typeof(int))
                        IntIndexer[column] = 0;
                    else if (datatype == typeof(long))
                        LongIndexer[column] = 0;
                    else if (datatype == typeof(decimal))
                        DecimalIndexer[column] = 0;
                    else if (datatype == typeof(double))
                        DoubleIndexer[column] = 0;
                    else if (datatype == typeof(float))
                        FloatIndexer[column] = 0;
                    else if (datatype == typeof(string))
                        StringIndexer[column] = string.Empty;
                    else if (datatype == typeof(bool))
                        BooleanIndexer[column] = false;
                    else if (datatype == typeof(DateTime))
                        DateTimeIndexer[column] = DateTime.Now;
                    else
                        WorkingValues[column] = default;
                }
                else
                {
                    WorkingValues[column] = default;
                }
            }
        }
    }

    public IEnumerable<(string ColumnName, Type? Datatype)> Columns =>
        _columnDbDatatypes.Select(c =>
        {
            var columnName = c.Key;
            var dbDatatype = c.Value;
            if (columnName != _identityColumn && DataTypeMapping.TryGetValue(dbDatatype, out var typeMapping))
            {
                return (columnName, typeMapping);
            }
            else
            {
                return (columnName, null as Type);
            }
        });

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns>null if there are no pending changes</returns>
    public (string Command, DynamicParameters Parameters, DataTableCommandType Type)? GetChangeSqlCommand(string schema, string table)
    {
        var nonIdentity = WorkingValues.Where(w => w.Key != _identityColumn).ToList();

        // Existing entity
        if (OriginalValues is not null && !ToBeDeleted)
        {
            var changes = nonIdentity.Where(w => w.Value?.ToString() != OriginalValues[w.Key]?.ToString()).ToList();
            // No changes => skip this record
            if (!changes.Any())
            {
                return null;
            }

            var parameters = new DynamicParameters();
            var builder = new StringBuilder();

            builder.Append("UPDATE [").Append(schema).Append("].[").Append(table).Append("] SET ");
            var j = 1;
            foreach (var value in changes)
            {
                builder.Append('[').Append(value.Key).Append(']').Append(" = @Working_").Append(j);
                parameters.Add($"Working_{j}", value.Value);
                if (j < changes.Count)
                {
                    builder.Append(',');
                }
                j++;
            }
            var k = 1;
            builder.Append(" WHERE ");
            foreach (var pk in _primaryKeyColumns)
            {
                var value = OriginalValues[pk];
                builder.Append('[').Append(pk).Append(']').Append(" = @Orig_").Append(k);
                parameters.Add($"Orig_{k}", value);
                if (k < _primaryKeyColumns.Count)
                {
                    builder.Append(" AND ");
                }
                k++;
            }

            return (builder.ToString(), parameters, DataTableCommandType.Update);
        }
        else if (OriginalValues is not null)
        {
            // Existing record to be deleted
            var builder = new StringBuilder();
            var parameters = new DynamicParameters();
            builder.Append("DELETE [").Append(schema).Append("].[").Append(table).Append("] WHERE ");
            var k = 1;
            foreach (var pk in _primaryKeyColumns)
            {
                var value = OriginalValues[pk];
                builder.Append('[').Append(pk).Append(']').Append(" = @Orig_").Append(k);
                parameters.Add($"Orig_{k}", value);
                if (k < _primaryKeyColumns.Count)
                {
                    builder.Append(" AND ");
                }
                k++;
            }

            return (builder.ToString(), parameters, DataTableCommandType.Delete);
        }
        else if (!ToBeDeleted)
        {
            // New entity
            var parameters = new DynamicParameters();
            var builder = new StringBuilder();
            builder
                .Append("INSERT INTO [")
                .Append(schema)
                .Append("].[")
                .Append(table)
                .Append("] (")
                .AppendJoin(',', nonIdentity.Select(k => $"[{k.Key}]"))
                .Append(") VALUES (");
            var j = 1;
            foreach (var value in nonIdentity) // do not include possible identity column in insert statement
            {
                builder.Append("@Working_").Append(j);
                parameters.Add($"Working_{j}", value.Value);
                if (j < nonIdentity.Count)
                {
                    builder.Append(',');
                }
                j++;
            }
            builder.Append(')');
            return (builder.ToString(), parameters, DataTableCommandType.Insert);
        }

        return null;
    }

    public static readonly Dictionary<string, Type> DataTypeMapping = new()
    {
        { "char", typeof(string) },
        { "varchar", typeof(string) },
        { "nchar", typeof(string) },
        { "nvarchar", typeof(string) },
        { "tinyint", typeof(byte) },
        { "smallint", typeof(short) },
        { "int", typeof(int) },
        { "bigint", typeof(long) },
        { "smallmoney", typeof(decimal) },
        { "money", typeof(decimal) },
        { "numeric", typeof(decimal) },
        { "decimal", typeof(decimal) },
        { "real", typeof(float) },
        { "float", typeof(double) },
        { "smalldatetime", typeof(DateTime) },
        { "datetime", typeof(DateTime) },
        { "datetime2", typeof(DateTime) },
        { "date", typeof(DateTime) },
        { "bit", typeof(bool) }
    };
}
