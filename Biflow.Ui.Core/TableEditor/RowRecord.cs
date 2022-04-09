using Dapper;
using System.Text;

namespace Biflow.Ui.Core;

public class RowRecord
{
    private Dictionary<string, string> _columnDbDatatypes;
    private HashSet<string> _primaryKeyColumns;

    public Dictionary<string, object?>? OriginalValues { get; }

    public Dictionary<string, object?> UnsupportedValues { get; } = new();

    public Dictionary<string, int?> IntValues { get; } = new();

    public Dictionary<string, string?> StringValues { get; } = new();

    public Dictionary<string, bool?> BooleanValues { get; } = new();

    public Dictionary<string, decimal?> DecimalValues { get; } = new();

    public Dictionary<string, DateTime?> DateTimeValues { get; } = new();

    public bool ToBeDeleted { get; set; }

    public RowRecord(Dictionary<string, string> columnDbDatatypes, HashSet<string> primaryKeyColumns, Dictionary<string, object?>? originalValues = null)
    {
        _columnDbDatatypes = columnDbDatatypes;
        _primaryKeyColumns = primaryKeyColumns;
        OriginalValues = originalValues;
        if (OriginalValues is not null)
        {
            foreach (var value in OriginalValues)
            {
                var datatype = GetColumnDatatype(value.Key);
                switch (datatype)
                {
                    case Datatype.Int:
                        IntValues[value.Key] = value.Value as int?;
                        break;
                    case Datatype.String:
                        StringValues[value.Key] = value.Value as string;
                        break;
                    case Datatype.Bool:
                        BooleanValues[value.Key] = value.Value as bool?;
                        break;
                    case Datatype.Decimal:
                        DecimalValues[value.Key] = value.Value as decimal?;
                        break;
                    case Datatype.DateTime:
                        DateTimeValues[value.Key] = value.Value as DateTime?;
                        break;
                    case Datatype.Unsupported:
                    default:
                        UnsupportedValues[value.Key] = value.Value;
                        break;
                }
            }
        }
        else
        {
            foreach (var column in _columnDbDatatypes.Keys)
            {
                var datatype = GetColumnDatatype(column);
                switch (datatype)
                {
                    case Datatype.Int:
                        IntValues[column] = 0;
                        break;
                    case Datatype.String:
                        StringValues[column] = "";
                        break;
                    case Datatype.Bool:
                        BooleanValues[column] = false;
                        break;
                    case Datatype.Decimal:
                        DecimalValues[column] = 0.0m;
                        break;
                    case Datatype.DateTime:
                        DateTimeValues[column] = DateTime.Now;
                        break;
                    case Datatype.Unsupported:
                    default:
                        UnsupportedValues[column] = default;
                        break;
                }
            }
        }
    }

    private Dictionary<string, object?> GetConsolidatedValues()
    {
        var dict = new Dictionary<string, object?>();
        foreach (var value in UnsupportedValues)
        {
            dict.Add(value.Key, value.Value);
        }
        foreach (var value in IntValues)
        {
            dict.Add(value.Key, value.Value);
        }
        foreach (var value in StringValues)
        {
            dict.Add(value.Key, value.Value);
        }
        foreach (var value in BooleanValues)
        {
            dict.Add(value.Key, value.Value);
        }
        foreach (var value in DecimalValues)
        {
            dict.Add(value.Key, value.Value);
        }
        foreach (var value in DateTimeValues)
        {
            dict.Add(value.Key, value.Value);
        }
        return dict;
    }

    public Datatype GetColumnDatatype(string column)
    {
        var dbDatatype = _columnDbDatatypes[column];
        if (dbDatatype.Contains("char"))
        {
            return Datatype.String;
        }
        else if (dbDatatype.Contains("int"))
        {
            return Datatype.Int;
        }
        else if (dbDatatype == "bit")
        {
            return Datatype.Bool;
        }
        else if (new string[] { "decimal", "float", "money", "numeric", "real", "smallmoney" }.Contains(dbDatatype))
        {
            return Datatype.Decimal;
        }
        else if (dbDatatype.Contains("date"))
        {
            return Datatype.DateTime;
        }
        else
        {
            return Datatype.Unsupported;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="table"></param>
    /// <returns>null if there are no pending changes</returns>
    public (string Command, DynamicParameters Parameters, DataTableCommandType Type)? GetChangeSqlCommand(string schema, string table)
    {
        var working = GetConsolidatedValues();

        // Existing entity
        if (OriginalValues is not null && !ToBeDeleted)
        {
            // No changes => skip this record
            if (working.Keys.All(k => working[k]?.ToString() == OriginalValues[k]?.ToString()))
            {
                return null;
            }

            var parameters = new DynamicParameters();
            var builder = new StringBuilder();

            builder.Append("UPDATE [").Append(schema).Append("].[").Append(table).Append("] SET ");
            var j = 1;
            foreach (var value in working)
            {
                builder.Append('[').Append(value.Key).Append(']').Append(" = @Working_").Append(j);
                parameters.Add($"Working_{j}", value.Value);
                if (j < working.Count)
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
                .AppendJoin(',', working.Keys.Select(k => $"[{k}]"))
                .Append(") VALUES (");
            var j = 1;
            foreach (var value in working)
            {
                builder.Append("@Working_").Append(j);
                parameters.Add($"Working_{j}", value.Value);
                if (j < working.Count)
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
}