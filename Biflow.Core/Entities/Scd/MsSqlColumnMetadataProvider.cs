using Dapper;
using Microsoft.Data.SqlClient;

namespace Biflow.Core.Entities;

public class MsSqlColumnMetadataProvider : IScdColumnMetadataProvider
{
    public async Task<IReadOnlyList<ScdColumnMetadata>> GetTableColumnsAsync(
        string connectionString,
        string schema,
        string table,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        const string sql = """
            select
              ColumnName = b.[name],
              Datatype = concat(
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
                      end),
              IsNullable = convert(bit, b.is_nullable)
            from sys.tables as a
              inner join sys.columns as b on a.object_id = b.object_id
              inner join sys.types as c on b.user_type_id = c.user_type_id
              inner join sys.schemas as d on a.schema_id = d.schema_id
            where a.[name] = @table and d.[name] = @schema
            order by b.[column_id]                                      
            """;
        var parameters = new { schema, table };
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<(string ColumnName, string DataType, bool IsNullable)>(command);
        return results
            .Select(r => new ScdColumnMetadata
            {
                ColumnName = r.ColumnName,
                DataType = r.DataType,
                IsNullable = r.IsNullable,
                IncludeInStagingTable = true,
                StagingTableExpression = null,
                TargetTableExpression = null
            })
            .ToArray();
    }
}