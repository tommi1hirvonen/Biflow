using System.Text;

namespace Biflow.Ui.TableEditor;

public class Row
{
    private readonly TableData _parentTable;
    private readonly IDictionary<string, object?>? _initialValues;
    private readonly string[] _upsertableColumns;
    private readonly string[] _primaryKeyColumns;

    private ObservableDictionary<string, object?> _values;
    private bool _valuesChanged = false;

    public IDictionary<string, object?> Values => _values;

    public ColumnValueIndexer<byte?> ByteIndexer { get; private set; }
    public ColumnValueIndexer<short?> ShortIndexer { get; private set; }
    public ColumnValueIndexer<int?> IntIndexer { get; private set; }
    public ColumnValueIndexer<long?> LongIndexer { get; private set; }
    public ColumnValueIndexer<decimal?> DecimalIndexer { get; private set; }
    public ColumnValueIndexer<double?> DoubleIndexer { get; private set; }
    public ColumnValueIndexer<float?> FloatIndexer { get; private set; }
    public ColumnValueIndexer<string?> StringIndexer { get; private set; }
    public ColumnValueIndexer<bool?> BooleanIndexer { get; private set; }
    public ColumnValueIndexer<DateTime?> DateTimeIndexer { get; private set; }
    public ColumnValueIndexer<DateOnly?> DateIndexer { get; private set; }
    public ColumnValueIndexer<TimeOnly?> TimeIndexer { get; private set; }

    internal bool ToBeDeleted { get; private set; }
    
    public bool IsUpdateable { get; }

    public bool IsNewRow { get; }

    public bool StickToTop { get; } = false;

    public bool HasChanges => _valuesChanged;

    public Row(Row other) : this(other._parentTable, other.IsUpdateable, other.Values.ToDictionary())
    {
        // Clear initial values after base constructor because this is in essence a new row.
        _initialValues = null;
        IsNewRow = true;
        var columnsToClear = _parentTable.Columns
            .Where(c => c.IsIdentity || c.IsComputed)
            .Select(c => c.Name)
            .ToArray();
        foreach (var column in columnsToClear)
        {
            Values[column] = default;
        }
    }

    public Row(
        TableData tableData,
        bool isUpdateable,
        IDictionary<string, object?>? initialValues = null)
    {
        _parentTable = tableData;
        _initialValues = initialValues;
        _values = _initialValues is not null ? new(_initialValues, OnValuesChanged) : new(OnValuesChanged);
        _upsertableColumns = _parentTable.Columns
            .Where(c => !c.IsIdentity && !c.IsComputed)
            .Select(c => c.Name)
            .ToArray();
        _primaryKeyColumns = _parentTable.Columns
            .Where(c => c.IsPrimaryKey)
            .Select(c => c.Name)
            .ToArray();

        IsUpdateable = isUpdateable;
        IsNewRow = initialValues is null;
        StickToTop = initialValues is null;

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
        DateIndexer = new(Values);
        TimeIndexer = new(Values);

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
                    else if (column.Datatype == typeof(DateOnly))
                        DateIndexer[column.Name] = DateOnly.FromDateTime(DateTime.Now);
                    else if (column.Datatype == typeof(TimeOnly))
                        TimeIndexer[column.Name] = TimeOnly.FromDateTime(DateTime.Now);
                    else
                        Values[column.Name] = default;
                }
            }
        }
    }

    private string QuotedSchemaAndTable =>
        $"{_parentTable.MasterDataTable.TargetSchemaName.QuoteName()}.{_parentTable.MasterDataTable.TargetTableName.QuoteName()}";

    private void OnValuesChanged()
    {
        _parentTable.HasChanges = true;
        _valuesChanged = Values.Any(HasChanged);
    }

    public void RevertChanges()
    {
        if (_initialValues is null)
        {
            return;
        }

        _values = new(_initialValues, OnValuesChanged);
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
        DateIndexer = new(Values);
        TimeIndexer = new(Values);
        _valuesChanged = false;
    }

    public void Delete()
    {
        if (!_parentTable.MasterDataTable.AllowDelete)
        {
            throw new InvalidOperationException("Deleting records is not allowed on this data table.");
        }
        OnValuesChanged();
        ToBeDeleted = true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns>null if there are no pending changes</returns>
    internal RowChangeSqlCommand? GetChangeSqlCommand()
    {
        // Existing entity
        if (_initialValues is not null && !ToBeDeleted && IsUpdateable)
        {
            var changes = Values.Where(HasChanged).ToArray();
                        
            // No changes => skip this record
            if (changes.Length == 0)
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
            foreach (var (pk, index) in _primaryKeyColumns.Select((pk, i) => (pk, i + 1)))
            {
                var value = _initialValues[pk];
                builder.Append(pk.QuoteName()).Append(" = @Orig_").Append(index);
                parameters.Add($"Orig_{index}", value);
                if (index < _primaryKeyColumns.Length)
                {
                    builder.Append(" AND ");
                }
            }

            return new(builder.ToString(), parameters, CommandType.Update);
        }
        else if (_initialValues is not null)
        {
            // Existing record to be deleted
            var builder = new StringBuilder();
            var parameters = new DynamicParameters();
            builder.Append("DELETE ").Append(QuotedSchemaAndTable).Append(" WHERE ");
            foreach (var (pk, index) in _primaryKeyColumns.Select((pk, i) => (pk, i + 1)))
            {
                var value = _initialValues[pk];
                builder.Append(pk.QuoteName()).Append(" = @Orig_").Append(index);
                parameters.Add($"Orig_{index}", value);
                if (index < _primaryKeyColumns.Length)
                {
                    builder.Append(" AND ");
                }
            }

            return new(builder.ToString(), parameters, CommandType.Delete);
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
                .AppendJoin(',', _upsertableColumns.Select(c => c.QuoteName()))
                .Append(") VALUES (");
            foreach (var (column, index) in _upsertableColumns.Select((c, i) => (c, i + 1))) // do not include possible identity column in insert statement
            {
                var value = Values[column];
                builder.Append("@Working_").Append(index);
                parameters.Add($"Working_{index}", value);
                if (index < _upsertableColumns.Length)
                {
                    builder.Append(',');
                }
            }
            builder.Append(')');
            return new(builder.ToString(), parameters, CommandType.Insert);
        }

        return null;
    }

    private bool HasChanged(KeyValuePair<string, object?> field) => _initialValues switch
    {
        not null => _upsertableColumns.Any(c => c == field.Key) && field.Value?.ToString() != _initialValues[field.Key]?.ToString(),
        _ => false
    };
        
}
