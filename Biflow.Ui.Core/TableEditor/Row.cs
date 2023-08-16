using Dapper;
using System.Text;

namespace Biflow.Ui.Core;

public class Row
{
    private readonly TableData _parentTable;
    private readonly IDictionary<string, object?>? _initialValues;
    private readonly ObservableDictionary<string, object?> _values;

    public IDictionary<string, object?> Values => _values;

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

    public bool ToBeDeleted { get; private set; }
    
    public bool IsUpdateable { get; }

    public Row(
        TableData tableData,
        bool isUpdateable,
        IDictionary<string, object?>? initialValues = null)
    {
        _parentTable = tableData;
        _initialValues = initialValues;
        _values = _initialValues is not null ? new(_initialValues, OnValuesChanged) : new(OnValuesChanged);

        IsUpdateable = isUpdateable;
        
        ByteIndexer = new(Values);
        ShortIndexer = new(Values);
        IntIndexer = new(Values);
        LongIndexer = new(Values);
        DecimalIndexer = new(Values);
        DoubleIndexer = new(Values);
        FloatIndexer = new(Values);
        StringIndexer = new(Values);
        BooleanIndexer = new(Values);
        DateTimeIndexer = new(Values);

        if (_initialValues is null)
        {
            foreach (var column in _parentTable.Columns)
            {
                if (column.IsIdentity)
                {
                    Values[column.Name] = default;
                }
                else
                {
                    if (column.Datatype == typeof(byte))
                        ByteIndexer[column.Name] = 0;
                    else if (column.Datatype == typeof(short))
                        ShortIndexer[column.Name] = 0;
                    else if (column.Datatype == typeof(int))
                        IntIndexer[column.Name] = 0;
                    else if (column.Datatype == typeof(long))
                        LongIndexer[column.Name] = 0;
                    else if (column.Datatype == typeof(decimal))
                        DecimalIndexer[column.Name] = 0;
                    else if (column.Datatype == typeof(double))
                        DoubleIndexer[column.Name] = 0;
                    else if (column.Datatype == typeof(float))
                        FloatIndexer[column.Name] = 0;
                    else if (column.Datatype == typeof(string))
                        StringIndexer[column.Name] = null;
                    else if (column.Datatype == typeof(bool))
                        BooleanIndexer[column.Name] = false;
                    else if (column.Datatype == typeof(DateTime))
                        DateTimeIndexer[column.Name] = DateTime.Now;
                    else
                        Values[column.Name] = default;
                }
            }
        }
    }

    private string QuotedSchemaAndTable =>
        $"{_parentTable.MasterDataTable.TargetSchemaName.QuoteName()}.{_parentTable.MasterDataTable.TargetTableName.QuoteName()}";

    private void OnValuesChanged() => _parentTable.HasChanges = true;

    public void Delete()
    {
        OnValuesChanged();
        ToBeDeleted = true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns>null if there are no pending changes</returns>
    public (string Command, DynamicParameters Parameters, DataTableCommandType Type)? GetChangeSqlCommand()
    {
        var upsertableColumns = _parentTable.Columns
            .Where(c => !c.IsIdentity && !c.IsComputed)
            .Select(c => c.Name)
            .ToArray();
        var primaryKey = _parentTable.Columns
            .Where(c => c.IsPrimaryKey)
            .Select(c => c.Name)
            .ToArray();

        // Existing entity
        if (_initialValues is not null && !ToBeDeleted && IsUpdateable)
        {
            var changes = Values
                .Where(w => upsertableColumns.Any(c => c == w.Key) && w.Value?.ToString() != _initialValues[w.Key]?.ToString())
                .ToArray();
                        
            // No changes => skip this record
            if (!changes.Any())
            {
                return null;
            }

            var parameters = new DynamicParameters();
            var builder = new StringBuilder();

            builder.Append("UPDATE ").Append(QuotedSchemaAndTable).Append(" SET ");
            foreach (var (value, index) in changes.Select((c, i) => (c, i + 1)))
            {
                builder.Append(value.Key.QuoteName()).Append(" = @Working_").Append(index);
                parameters.Add($"Working_{index}", value.Value);
                if (index < changes.Length)
                {
                    builder.Append(',');
                }
            }
            builder.Append(" WHERE ");
            foreach (var (pk, index) in primaryKey.Select((pk, i) => (pk, i + 1)))
            {
                var value = _initialValues[pk];
                builder.Append(pk.QuoteName()).Append(" = @Orig_").Append(index);
                parameters.Add($"Orig_{index}", value);
                if (index < primaryKey.Length)
                {
                    builder.Append(" AND ");
                }
            }

            return (builder.ToString(), parameters, DataTableCommandType.Update);
        }
        else if (_initialValues is not null)
        {
            // Existing record to be deleted
            var builder = new StringBuilder();
            var parameters = new DynamicParameters();
            builder.Append("DELETE ").Append(QuotedSchemaAndTable).Append(" WHERE ");
            foreach (var (pk, index) in primaryKey.Select((pk, i) => (pk, i + 1)))
            {
                var value = _initialValues[pk];
                builder.Append(pk.QuoteName()).Append(" = @Orig_").Append(index);
                parameters.Add($"Orig_{index}", value);
                if (index < primaryKey.Length)
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
                .Append("INSERT INTO ")
                .Append(QuotedSchemaAndTable)
                .Append(" (")
                .AppendJoin(',', upsertableColumns.Select(c => c.QuoteName()))
                .Append(") VALUES (");
            foreach (var (column, index) in upsertableColumns.Select((c, i) => (c, i + 1))) // do not include possible identity column in insert statement
            {
                var value = Values[column];
                builder.Append("@Working_").Append(index);
                parameters.Add($"Working_{index}", value);
                if (index < upsertableColumns.Length)
                {
                    builder.Append(',');
                }
            }
            builder.Append(')');
            return (builder.ToString(), parameters, DataTableCommandType.Insert);
        }

        return null;
    }
}
