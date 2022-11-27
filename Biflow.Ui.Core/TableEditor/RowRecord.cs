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
            foreach (var column in _dataset.Columns)
            {
                //var column = columnInfo.Name;
                //var dbDatatype = columnInfo.DbDatatype;
                if (column.IsIdentity)
                {
                    WorkingValues[column.Name] = default;
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
                        WorkingValues[column.Name] = default;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns>null if there are no pending changes</returns>
    public (string Command, DynamicParameters Parameters, DataTableCommandType Type)? GetChangeSqlCommand()
    {
        var (schema, table) = (_dataset.DataTable.TargetSchemaName, _dataset.DataTable.TargetTableName);

        var upsertableColumns = _dataset.Columns
            .Where(c => !c.IsIdentity && !c.IsComputed)
            .Select(c => c.Name)
            .ToList();
        var primaryKey = _dataset.Columns
            .Where(c => c.IsPrimaryKey)
            .Select(c => c.Name)
            .ToList();

        // Existing entity
        if (_originalValues is not null && !ToBeDeleted)
        {
            var changes = WorkingValues
                .Where(w => upsertableColumns.Any(c => c == w.Key) && w.Value?.ToString() != _originalValues[w.Key]?.ToString())
                .ToList();
                        
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
            foreach (var (pk, index) in primaryKey.Select((pk, i) => (pk, i + 1)))
            {
                var value = _originalValues[pk];
                builder.Append('[').Append(pk).Append(']').Append(" = @Orig_").Append(index);
                parameters.Add($"Orig_{index}", value);
                if (index < primaryKey.Count)
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
            foreach (var (pk, index) in primaryKey.Select((pk, i) => (pk, i + 1)))
            {
                var value = _originalValues[pk];
                builder.Append('[').Append(pk).Append(']').Append(" = @Orig_").Append(index);
                parameters.Add($"Orig_{index}", value);
                if (index < primaryKey.Count)
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
                .AppendJoin(',', upsertableColumns.Select(c => $"[{c}]"))
                .Append(") VALUES (");
            foreach (var (column, index) in upsertableColumns.Select((c, i) => (c, i + 1))) // do not include possible identity column in insert statement
            {
                var value = WorkingValues[column];
                builder.Append("@Working_").Append(index);
                parameters.Add($"Working_{index}", value);
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
}
