using Biflow.DataAccess.Models;
using Dapper;
using System.Text;

namespace Biflow.Ui.Core;

public class RowRecord
{
    private readonly Dataset _dataset;
    private readonly IDictionary<string, object?>? _originalValues;

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
        Dataset dataset,
        IDictionary<string, object?>? originalValues = null)
    {
        _dataset= dataset;
        _originalValues = originalValues;
        WorkingValues = _originalValues is not null ? new(_originalValues) : new();
        
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

        if (_originalValues is null)
        {
            foreach (var columnInfo in _dataset.ColumnDbDataTypes)
            {
                var column = columnInfo.Key;
                var dbDatatype = columnInfo.Value.BaseDataType;
                if (column != _dataset.IdentityColumn && DataTypeMapping.TryGetValue(dbDatatype, out var datatype))
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
                        StringIndexer[column] = null;
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

    public IEnumerable<(string ColumnName, Type? Datatype, IEnumerable<(object? Value, object? DisplayValue)>? LookupValues)> Columns =>
        _dataset.ColumnDbDataTypes.Select(c =>
        {
            var columnName = c.Key;
            var dbDatatype = c.Value.BaseDataType;
            var isComputed = c.Value.IsComputed;
            var lookupValues = _dataset.LookupData.GetValueOrDefault(columnName);
            if (columnName != _dataset.IdentityColumn && !isComputed && DataTypeMapping.TryGetValue(dbDatatype, out var typeMapping))
            {
                return (columnName, typeMapping, lookupValues);
            }
            else
            {
                return (columnName, null as Type, null as IEnumerable<(object? Value, object? DisplayValue)>);
            }
        });

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns>null if there are no pending changes</returns>
    public (string Command, DynamicParameters Parameters, DataTableCommandType Type)? GetChangeSqlCommand()
    {
        var (schema, table) = (_dataset.DataTable.TargetSchemaName, _dataset.DataTable.TargetTableName);
        var computedColumns = _dataset.ColumnDbDataTypes
            .Where(c => c.Value.IsComputed)
            .Select(c => c.Key)
            .ToList();
        var upsertableColumns = WorkingValues
            .Where(w => w.Key != _dataset.IdentityColumn)
            .Where(w => !computedColumns.Any(c => c == w.Key))
            .ToList();

        // Existing entity
        if (_originalValues is not null && !ToBeDeleted)
        {
            var changes = upsertableColumns.Where(w => w.Value?.ToString() != _originalValues[w.Key]?.ToString()).ToList();
            // No changes => skip this record
            if (!changes.Any())
            {
                return null;
            }

            var parameters = new DynamicParameters();
            var builder = new StringBuilder();

            builder.Append("UPDATE [").Append(schema).Append("].[").Append(table).Append("] SET ");
            foreach (var (value, index) in changes.Select((c, i) => (c, i + 1)))
            {
                builder.Append('[').Append(value.Key).Append(']').Append(" = @Working_").Append(index);
                parameters.Add($"Working_{index}", value.Value);
                if (index < changes.Count)
                {
                    builder.Append(',');
                }
            }
            builder.Append(" WHERE ");
            foreach (var (pk, index) in _dataset.PrimaryKeyColumns.Select((pk, i) => (pk, i + 1)))
            {
                var value = _originalValues[pk];
                builder.Append('[').Append(pk).Append(']').Append(" = @Orig_").Append(index);
                parameters.Add($"Orig_{index}", value);
                if (index < _dataset.PrimaryKeyColumns.Count)
                {
                    builder.Append(" AND ");
                }
            }

            return (builder.ToString(), parameters, DataTableCommandType.Update);
        }
        else if (_originalValues is not null)
        {
            // Existing record to be deleted
            var builder = new StringBuilder();
            var parameters = new DynamicParameters();
            builder.Append("DELETE [").Append(schema).Append("].[").Append(table).Append("] WHERE ");
            foreach (var (pk, index) in _dataset.PrimaryKeyColumns.Select((pk, i) => (pk, i + 1)))
            {
                var value = _originalValues[pk];
                builder.Append('[').Append(pk).Append(']').Append(" = @Orig_").Append(index);
                parameters.Add($"Orig_{index}", value);
                if (index < _dataset.PrimaryKeyColumns.Count)
                {
                    builder.Append(" AND ");
                }
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
                .AppendJoin(',', upsertableColumns.Select(k => $"[{k.Key}]"))
                .Append(") VALUES (");
            foreach (var (value, index) in upsertableColumns.Select((c, i) => (c, i + 1))) // do not include possible identity column in insert statement
            {
                builder.Append("@Working_").Append(index);
                parameters.Add($"Working_{index}", value.Value);
                if (index < upsertableColumns.Count)
                {
                    builder.Append(',');
                }
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
