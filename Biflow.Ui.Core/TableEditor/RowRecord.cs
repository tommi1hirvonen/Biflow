using Dapper;
using System.Text;

namespace Biflow.Ui.Core;

public class RowRecord
{
    private readonly Dictionary<string, string> _columnDbDatatypes;
    private readonly HashSet<string> _primaryKeyColumns;
    private readonly string? _identityColumn;

    public Dictionary<string, object?>? OriginalValues { get; }

    public Dictionary<string, object?> UnsupportedValues { get; } = new();

    public Dictionary<string, byte?> ByteValues { get; } = new();
    public Dictionary<string, short?> ShortValues { get; } = new();
    public Dictionary<string, int?> IntValues { get; } = new();
    public Dictionary<string, long?> LongValues { get; } = new();
    public Dictionary<string, decimal?> DecimalValues { get; } = new();
    public Dictionary<string, double?> DoubleValues { get; } = new();
    public Dictionary<string, float?> FloatValues { get; } = new();
    public Dictionary<string, string?> StringValues { get; } = new();
    public Dictionary<string, bool?> BooleanValues { get; } = new();
    public Dictionary<string, DateTime?> DateTimeValues { get; } = new();

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
        if (OriginalValues is not null)
        {
            foreach (var record in OriginalValues)
            {
                var column = record.Key;
                var value = record.Value;
                var dbDatatype = _columnDbDatatypes[column];
                if (column != _identityColumn && DataTypeMapping.TryGetValue(dbDatatype, out var datatype))
                {
                    if (datatype == typeof(byte))
                        ByteValues[column] = value as byte?;
                    else if (datatype == typeof(short))
                        ShortValues[column] = value as short?;
                    else if (datatype == typeof(int))
                        IntValues[column] = value as int?;
                    else if (datatype == typeof(long))
                        LongValues[column] = value as long?;
                    else if (datatype == typeof(decimal))
                        DecimalValues[column] = value as decimal?;
                    else if (datatype == typeof(double))
                        DoubleValues[column] = value as double?;
                    else if (datatype == typeof(float))
                        FloatValues[column] = value as float?;
                    else if (datatype == typeof(string))
                        StringValues[column] = value as string;
                    else if (datatype == typeof(bool))
                        BooleanValues[column] = value as bool?;
                    else if (datatype == typeof(DateTime))
                        DateTimeValues[column] = value as DateTime?;
                    else
                        UnsupportedValues[column] = value;
                }
                else
                {
                    UnsupportedValues[column] = value;
                }
            }
        }
        else
        {
            foreach (var columnInfo in _columnDbDatatypes)
            {
                var column = columnInfo.Key;
                var dbDatatype = columnInfo.Value;
                if (column != _identityColumn && DataTypeMapping.TryGetValue(dbDatatype, out var datatype))
                {
                    if (datatype == typeof(byte))
                        ByteValues[column] = 0;
                    else if (datatype == typeof(short))
                        ShortValues[column] = 0;
                    else if (datatype == typeof(int))
                        IntValues[column] = 0;
                    else if (datatype == typeof(long))
                        LongValues[column] = 0;
                    else if (datatype == typeof(decimal))
                        DecimalValues[column] = 0;
                    else if (datatype == typeof(double))
                        DoubleValues[column] = 0;
                    else if (datatype == typeof(float))
                        FloatValues[column] = 0;
                    else if (datatype == typeof(string))
                        StringValues[column] = string.Empty;
                    else if (datatype == typeof(bool))
                        BooleanValues[column] = false;
                    else if (datatype == typeof(DateTime))
                        DateTimeValues[column] = DateTime.Now;
                    else
                        UnsupportedValues[column] = default;
                }
                else
                {
                    UnsupportedValues[column] = default;
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

    private IEnumerable<KeyValuePair<string, object?>> ConsolidatedValues =>
        UnsupportedValues
            .Concat(ByteValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(ShortValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(IntValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(LongValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(DecimalValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(DoubleValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(FloatValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(StringValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(BooleanValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)))
            .Concat(DateTimeValues.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value)));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns>null if there are no pending changes</returns>
    public (string Command, DynamicParameters Parameters, DataTableCommandType Type)? GetChangeSqlCommand(string schema, string table)
    {
        var nonIdentity = ConsolidatedValues.Where(w => w.Key != _identityColumn).ToList();

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