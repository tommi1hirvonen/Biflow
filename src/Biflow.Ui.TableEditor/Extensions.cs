namespace Biflow.Ui.TableEditor;

public static class Extensions
{
    public static async Task<IEnumerable<string>> GetColumnNamesAsync(this MasterDataTable table)
    {
        await using var connection = new SqlConnection(table.Connection.ConnectionString);
        return await table.Connection.RunImpersonatedOrAsCurrentUserAsync(async () =>
        {
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
        });
    }

    public static async Task<TableData> LoadDataAsync(this MasterDataTable table, int? top = null, FilterSet? filters = null)
    {
        if (table.Lookups.Any(lookup => lookup.LookupTable.ConnectionId != table.Connection.ConnectionId))
        {
            throw new InvalidOperationException("All lookup tables must use the same connection as the main table.");
        }

        await using var connection = new SqlConnection(table.Connection.ConnectionString);
        return await table.Connection.RunImpersonatedOrAsCurrentUserAsync(async () =>
        {
            await connection.OpenAsync();

            var columns = (await table.GetColumnsAsync(connection)).ToArray();

            var (query, parameters) = new DataTableQueryBuilder(
                table: table,
                top: top + 1,
                filters: filters)
                .Build();
            var rows = (List<dynamic>)await connection.QueryAsync(query, parameters);
            var initialValues = rows
                .TakeIfNotNull(top)
                .Cast<IDictionary<string, object?>>()
                .ToArray();

            var hasMoreRows = top is not null && rows.Count > top; // Actual type of rows is List, so Count() is safe to do. O(1) operation
            return new TableData(table, columns, initialValues, hasMoreRows);
        });  
    }

    internal static async Task<IEnumerable<Column>> GetColumnsAsync(this MasterDataTable table, bool includeLookups = true)
    {
        await using var connection = new SqlConnection(table.Connection.ConnectionString);
        return await table.Connection.RunImpersonatedOrAsCurrentUserAsync(async () =>
        {
            await connection.OpenAsync();
            return await table.GetColumnsAsync(connection, includeLookups);
        });
    }

    private static async Task<IEnumerable<Column>> GetColumnsAsync(this MasterDataTable table, SqlConnection connection, bool includeLookups = true)
    {
        var primaryKeyColumns = (await table.GetPrimaryKeyAsync(connection)).ToHashSet();
        var identityColumn = await table.GetIdentityColumnOrNullAsync(connection);
        var columnDatatypes = await table.GetColumnDatatypesAsync(connection);

        var lookups = includeLookups ? await table.GetLookupsAsync() : null;
        var columns = columnDatatypes.Select(c =>
        {
            var isPk = primaryKeyColumns.Contains(c.Name);
            var isIdentity = identityColumn == c.Name;
            var isLocked = table.LockedColumnsExcludeMode ? !table.LockedColumns.Contains(c.Name) : table.LockedColumns.Contains(c.Name);
            var isHidden = table.HiddenColumns.Contains(c.Name);
            var lookup = lookups?.GetValueOrDefault(c.Name);
            var datatype = DatatypeMapping.GetValueOrDefault(c.Datatype);
            return new Column(
                Name: c.Name,
                IsPrimaryKey: isPk,
                IsIdentity: isIdentity,
                IsComputed: c.Computed,
                IsLocked: isLocked,
                IsHidden: isHidden,
                DbDatatype: c.Datatype,
                DbDatatypeDescription: c.DatatypeDesc,
                DbCreateDatatype: c.CreateDatatype,
                Datatype: datatype,
                Lookup: lookup);
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
            await using var connection = new SqlConnection(lookup.LookupTable.Connection.ConnectionString);
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
                SELECT {QuoteName(lookup.LookupValueColumn)}, {QuoteName(lookup.LookupDescriptionColumn)}
                FROM {QuoteName(lookup.LookupTable.TargetSchemaName)}.{QuoteName(lookup.LookupTable.TargetTableName)}
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
                return new LookupValue(value.Value, displayValue);
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
                            when type_name(b.user_type_id) like N'%char' or type_name(b.user_type_id) like '%binary'
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
                            --types with length specification
                            when type_name(b.user_type_id) like N'n%char'
                                then concat(N'(', case b.max_length when -1 then N'max' else cast(b.max_length / 2 as nvarchar(20)) end, N')')
                            when type_name(b.user_type_id) like N'%char' or type_name(b.user_type_id) like '%binary'
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
        { "date", typeof(DateOnly) },
        { "time", typeof(TimeOnly) },
        { "bit", typeof(bool) }
    };

    private const int SysnameLength = 128;

    /// <summary>
    /// Returns a string with the delimiters added to make the input string
    /// a valid SQL Server delimited identifier. Unlike the T-SQL version,
    /// an ArgumentException is thrown instead of returning a null for
    /// invalid arguments.
    /// </summary>
    /// <param name="name">sysname, limited to 128 characters.</param>
    /// <param name="quoteCharacter">Can be a single quotation mark ( ' ), a
    /// left or right bracket ( [] ), or a double quotation mark ( " ).</param>
    /// <returns>An escaped identifier, no longer than 258 characters.</returns>
    internal static string QuoteName(this string name, char quoteCharacter = '[') => (name, quoteCharacter) switch
    {
        ({ Length: > SysnameLength }, _) => throw new ArgumentException($"{nameof(name)} is longer than {SysnameLength} characters"),
        (_, '\'') => $"'{name.Replace("'", "''")}'",
        (_, '"') => $"\"{name.Replace("\"", "\"\"")}\"",
        (_, '[' or ']') => $"[{name.Replace("]", "]]")}]",
        _ => throw new ArgumentException("quoteCharacter must be one of: ', \", [, or ]")
    };

    private static IEnumerable<TResult> TakeIfNotNull<TResult>(this IEnumerable<TResult> source, int? count)
    {
        return count.HasValue ? source.Take(count.Value) : source;
    }

    internal static bool ContainsIgnoreCase(this string source, string? toCheck) => toCheck switch
    {
        not null => source.Contains(toCheck, StringComparison.OrdinalIgnoreCase),
        _ => false
    };
}
