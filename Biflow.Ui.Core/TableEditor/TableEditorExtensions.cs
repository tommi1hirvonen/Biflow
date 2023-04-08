using Biflow.DataAccess.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

public static class TableEditorExtensions
{
    public static async Task<IEnumerable<string>> GetColumnNamesAsync(this MasterDataTable table)
    {
        using var connection = new SqlConnection(table.Connection.ConnectionString);
        await connection.OpenAsync();
        var columns = await connection.QueryAsync<string>("""
            select b.[name]
            from sys.tables as a
                inner join sys.columns as b on a.object_id = b.object_id
                inner join sys.schemas as d on a.schema_id = d.schema_id
            where a.[name] = @TableName and d.[name] = @SchemaName
            order by b.[column_id]
            """,
            new { TableName = table.TargetTableName, SchemaName = table.TargetSchemaName }
        );
        return columns;
    }

    public static async Task<TableData> LoadDataAsync(this MasterDataTable table, int? top = null, FilterSet? filters = null)
    {
        if (table.Lookups.Any(lookup => lookup.LookupTable.ConnectionId != table.Connection.ConnectionId))
        {
            throw new InvalidOperationException("All lookup tables must use the same connection as the main table.");
        }

        top ??= 1000;

        using var connection = new SqlConnection(table.Connection.ConnectionString);
        await connection.OpenAsync();

        var columns = (await table.GetColumnsAsync(connection)).ToHashSet();

        var (query, parameters) = new DataTableQueryBuilder(table, (int)top, filters).Build();
        var rows = await connection.QueryAsync(query, parameters);
        var initialValues = rows.Cast<IDictionary<string, object?>>();
        
        return new TableData(table, columns, initialValues);
    }

    internal static async Task<IEnumerable<Column>> GetColumnsAsync(this MasterDataTable table, bool includeLookups = true)
    {
        using var connection = new SqlConnection(table.Connection.ConnectionString);
        await connection.OpenAsync();
        return await table.GetColumnsAsync(connection, includeLookups);
    }

    internal static async Task<IEnumerable<Column>> GetColumnsAsync(this MasterDataTable table, SqlConnection connection, bool includeLookups = true)
    {
        var primaryKeyColumns = (await table.GetPrimaryKeyAsync(connection)).ToHashSet();
        var identityColumn = await table.GetIdentityColumnOrNullAsync(connection);
        var columnDatatypes = await table.GetColumnDatatypesAsync(connection);

        var lookups = includeLookups ? await table.GetLookupsAsync() : null;
        var columns = columnDatatypes.Select(c =>
        {
            var isPk = primaryKeyColumns.Contains(c.Name);
            var isIdentity = identityColumn == c.Name;
            var isLocked = table.LockedColumns.Contains(c.Name);
            var lookup = lookups?.GetValueOrDefault(c.Name);
            var datatype = DatatypeMapping.GetValueOrDefault(c.Datatype);
            return new Column(
                name: c.Name,
                isPrimaryKey: isPk,
                isIdentity: isIdentity,
                isComputed: c.Computed,
                isLocked: isLocked,
                dbDatatype: c.Datatype,
                dbDatatypeDescription: c.DatatypeDesc,
                dbCreateDatatype: c.CreateDatatype,
                datatype: datatype,
                lookup: lookup);
        });
        return columns;
    }

    private static Task<IEnumerable<string>> GetPrimaryKeyAsync(this MasterDataTable table, SqlConnection connection) =>
        connection.QueryAsync<string>("""
            select
                c.[name]
            from sys.index_columns as a
                inner join sys.indexes as b on a.index_id = b.index_id and a.object_id = b.object_id 
                inner join sys.columns as c on a.object_id = c.object_id and a.column_id = c.column_id
                inner join sys.tables as d on a.object_id = d.object_id
                inner join sys.schemas as e on d.schema_id = e.schema_id
            where b.is_primary_key = 1 and d.[name] = @TableName and e.[name] = @SchemaName
            """,
            new { TableName = table.TargetTableName, SchemaName = table.TargetSchemaName }
        );

    private static Task<string?> GetIdentityColumnOrNullAsync(this MasterDataTable table, SqlConnection connection) =>
        connection.ExecuteScalarAsync<string?>("""
            select top 1 a.[name]
            from sys.columns as a
                inner join sys.tables as b on a.object_id = b.object_id
                inner join sys.schemas as c on b.schema_id = c.schema_id
            where a.is_identity = 1 and c.[name] = @SchemaName and b.[name] = @TableName
            """,
            new { TableName = table.TargetTableName, SchemaName = table.TargetSchemaName }
        );

    private static async Task<Dictionary<string, Lookup>> GetLookupsAsync(this MasterDataTable table) =>
        await table.Lookups.ToAsyncEnumerable().SelectAwait(async lookup =>
        {
            using var connection = new SqlConnection(lookup.LookupTable.Connection.ConnectionString);
            await connection.OpenAsync();

            var dataTypes = await lookup.LookupTable.GetColumnDatatypesAsync(connection);

            var lookupDisplayValueDatatype = lookup.LookupDisplayType switch
            {
                LookupDisplayType.Value =>
                    DatatypeMapping.GetValueOrDefault(dataTypes.First(dt => dt.Name == lookup.LookupValueColumn).Datatype),
                LookupDisplayType.Description =>
                    DatatypeMapping.GetValueOrDefault(dataTypes.First(dt => dt.Name == lookup.LookupDescriptionColumn).Datatype),
                LookupDisplayType.ValueAndDescription => typeof(string),
                _ => null
            };
            ArgumentNullException.ThrowIfNull(lookupDisplayValueDatatype);

            var results = await connection.QueryAsync<(object? Value, object? Description)>($"""
                SELECT [{lookup.LookupValueColumn}], [{lookup.LookupDescriptionColumn}]
                FROM [{lookup.LookupTable.TargetSchemaName}].[{lookup.LookupTable.TargetTableName}]
                """);

            var data = results.Select(value =>
            {
                var displayValue = lookup.LookupDisplayType switch
                {
                    LookupDisplayType.Value => value.Value,
                    LookupDisplayType.Description => value.Description,
                    LookupDisplayType.ValueAndDescription => string.Join(' ', value.Value, value.Description),
                    _ => value.Description
                };
                return (value.Value, displayValue);
            });

            return (lookup.ColumnName, new Lookup(lookup, lookupDisplayValueDatatype, data));
        }).ToDictionaryAsync(key => key.ColumnName, value => value.Item2);

    private static Task<IEnumerable<(string Name, string Datatype, string DatatypeDesc, string CreateDatatype, bool Computed)>>
        GetColumnDatatypesAsync(this MasterDataTable table, SqlConnection connection) =>
        connection.QueryAsync<(string Name, string Datatype, string DatatypeDesc, string CreateDatatype, bool Computed)>(
            """
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
                            --types with length specification
                            when type_name(b.user_type_id) like N'n%char'
                                then concat(N'(', case b.max_length when -1 then N'max' else cast(b.max_length / 2 as nvarchar(20)) end, N')')
                            when type_name(b.user_type_id) like N'%char'
                                then concat(N'(', case b.max_length when -1 then N'max' else cast(b.max_length as nvarchar(20)) end, N')')
                        end,
                        case when b.is_identity = 1 then concat(N' identity(', ident_seed(d.name + '.' + a.name), ', ', ident_incr(d.name + '.' + a.name), ')') end,
                        case when b.is_nullable = 1 then N' null' else N' not null' end
                    ),
                CreateTableDatatype = concat(
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
                            when type_name(b.user_type_id) like N'n%char'
                                then concat(N'(', case b.max_length when -1 then N'max' else cast(b.max_length / 2 as nvarchar(20)) end, N')')
                            when type_name(b.user_type_id) like N'%char'
                                then concat(N'(', case b.max_length when -1 then N'max' else cast(b.max_length as nvarchar(20)) end, N')')
                        end),
                IsComputed = b.is_computed
            from sys.tables as a
                inner join sys.columns as b on a.object_id = b.object_id
                inner join sys.types as c on b.user_type_id = c.user_type_id
                inner join sys.schemas as d on a.schema_id = d.schema_id
            where a.[name] = @TableName and d.[name] = @SchemaName
            order by b.[column_id]
            """,
            new { TableName = table.TargetTableName, SchemaName = table.TargetSchemaName });

    public static readonly Dictionary<string, Type> DatatypeMapping = new()
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
