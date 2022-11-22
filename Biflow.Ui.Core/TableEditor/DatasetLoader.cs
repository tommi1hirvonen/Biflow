using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;

namespace Biflow.Ui.Core;

public class DatasetLoader
{
    internal string ConnectionString { get; }
    internal string Schema { get; }
    internal string Table { get; }

    public DatasetLoader(string connectionString, string schema, string table)
    {
        ConnectionString = connectionString;
        Schema = schema;
        Table = table;
    }

    public async Task<Dataset> LoadDataAsync(int? top = null, FilterSet? filters = null)
    {
        top ??= 1000;

        using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        var primaryKeyColumns = (await connection.QueryAsync<string>("""
            select
                c.[name]
            from sys.index_columns as a
                inner join sys.indexes as b on a.index_id = b.index_id and a.object_id = b.object_id 
                inner join sys.columns as c on a.object_id = c.object_id and a.column_id = c.column_id
                inner join sys.tables as d on a.object_id = d.object_id
                inner join sys.schemas as e on d.schema_id = e.schema_id
            where b.is_primary_key = 1 and d.[name] = @TableName and e.[name] = @SchemaName
            """,
            new { TableName = Table, SchemaName = Schema }
        )).ToHashSet();

        var identityColumn = await connection.ExecuteScalarAsync<string?>("""
            select top 1 a.[name]
            from sys.columns as a
                inner join sys.tables as b on a.object_id = b.object_id
                inner join sys.schemas as c on b.schema_id = c.schema_id
            where a.is_identity = 1 and c.[name] = @SchemaName and b.[name] = @TableName
            """,
            new { TableName = Table, SchemaName = Schema }
        );

        var columnDatatypes = await connection.QueryAsync<(string, string, string)>("""
            select
                ColumnName = b.[name],
                DataType = c.[name],
                DataTypeDescription = concat(
                        type_name(b.user_type_id),
                        case
                            --types with precision and scale specification
                            when type_name(b.user_type_id) in (N'decimal', N'numeric')
                                then concat(N'(', b.precision, N',', b.scale, N')')
                            --types with scale specification only
                            when type_name(b.user_type_id) in (N'time', N'datetime2', N'datetimeoffset') 
                                then concat(N'(', b.scale, N')')
                            --float default precision is 53 - add precision when column has a different precision value
                            when type_name(b.user_type_id) in (N'float')
                                then case when b.precision = 53 then N'' else concat(N'(', b.precision, N')') end
                            --types with length specifiecation
                            when type_name(b.user_type_id) like N'%char'
                                then concat(N'(', case b.max_length when -1 then N'max' else cast(b.max_length as nvarchar(20)) end, N')')
                        end,
                        case when b.is_identity = 1 then concat(N' identity(', ident_seed(d.name + '.' + a.name), ', ', ident_incr(d.name + '.' + a.name), ')') end,
                        case when b.is_nullable = 1 then N' null' else N' not null' end
                    )
            from sys.tables as a
                inner join sys.columns as b on a.object_id = b.object_id
                inner join sys.types as c on b.user_type_id = c.user_type_id
                inner join sys.schemas as d on a.schema_id = d.schema_id
            where a.[name] = @TableName and d.[name] = @SchemaName
            """,
            new { TableName = Table, SchemaName = Schema }
        );

        var columnDbDataTypes = columnDatatypes.ToDictionary(key => key.Item1, value => new DbDataType(value.Item2, value.Item3));

        var cmdBuilder = new StringBuilder();
        var parameters = new DynamicParameters();

        cmdBuilder.Append("SELECT TOP ").Append(top).Append(" * FROM [").Append(Schema).Append("].[").Append(Table).Append(']');

        if (filters?.Filters.Any(f => f.Value.Enabled1) ?? false)
        {
            cmdBuilder.Append(" WHERE ");
            var index = 1;
            foreach (var (column, filter) in filters.Filters.Where(f => f.Value.Enabled1))
            {
                if (index > 1)
                {
                    cmdBuilder.Append(" AND ");
                }
                cmdBuilder.Append('(');
                var (statement1, paramsToAdd1) = GenerateFilterStatement(column, filter.Operator1, filter.FilterValue1, index);
                cmdBuilder.Append(statement1);
                parameters.AddDynamicParams(paramsToAdd1);
                if (filter.Enabled2)
                {
                    index++;
                    var operand = filter.AndOr ? " AND " : " OR ";
                    cmdBuilder.Append(operand);
                    var (statement2, paramsToAdd2) = GenerateFilterStatement(column, filter.Operator2, filter.FilterValue2, index);
                    cmdBuilder.Append(statement2);
                    parameters.AddDynamicParams(paramsToAdd2);
                }
                cmdBuilder.Append(')');
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

        return new Dataset(this, primaryKeyColumns, identityColumn, columnDbDataTypes, originalData);
    }

    private static (string Statement, DynamicParameters Params) GenerateFilterStatement(string column, Enum oper, object filterValue, int index)
    {
        var statementBuilder = new StringBuilder();
        var parameters = new DynamicParameters();
        statementBuilder.Append(" [").Append(column).Append("] ");
        if (oper is NumberFilterOperator nfo)
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
                statementBuilder.Append(operatorText);
            }
            else
            {
                statementBuilder.Append(operatorText).Append("@Parameter_").Append(index);
                parameters.Add($"Parameter_{index}", filterValue);
            }
        }
        else if (oper is TextFilterOperator tfo)
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
            var value = filterValue;
            if (tfo == TextFilterOperator.IsBlank || tfo == TextFilterOperator.IsNotBlank)
            {
                statementBuilder.Append(operatorText);
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
                statementBuilder.Append(operatorText).Append("@Parameter_").Append(index);
                parameters.Add($"Parameter_{index}", value);
            }
        }
        else if (oper is BooleanFilterOperator bfo)
        {
            var operatorText = bfo switch
            {
                BooleanFilterOperator.Equals => " = ",
                _ => throw new ArgumentException($"Unsupported BooleanFilterOperator value {bfo}")
            };
            statementBuilder.Append(operatorText).Append("@Parameter_").Append(index);
            parameters.Add($"Parameter_{index}", filterValue);
        }
        else
        {
            throw new ArgumentException($"Unsupported filter operator type {oper.GetType()}");
        }
        return (statementBuilder.ToString(), parameters);
    }
    
}
