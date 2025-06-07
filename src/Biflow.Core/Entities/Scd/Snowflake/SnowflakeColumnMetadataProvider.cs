using Dapper;
using Snowflake.Data.Client;

namespace Biflow.Core.Entities.Scd.Snowflake;

internal class SnowflakeColumnMetadataProvider(string connectionString) : IColumnMetadataProvider
{
    public async Task<IReadOnlyList<FullColumnMetadata>> GetColumnsAsync(
        string schema, string table, CancellationToken cancellationToken = default)
    {
        await using var connection = new SnowflakeDbConnection(connectionString);
        
        var ordinalsCmd = new CommandDefinition("""
            SELECT COLUMN_NAME, ORDINAL_POSITION
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = :schema AND TABLE_NAME = :table;
            """,
            parameters: new { schema, table },
            cancellationToken: cancellationToken);

        var ordinals = (List<dynamic>)await connection.QueryAsync(ordinalsCmd);
        
        if (ordinals.Count == 0)
        {
            return [];
        }
        
        var ordinalMap = ordinals.ToDictionary(k => k.COLUMN_NAME, v => (int)v.ORDINAL_POSITION);
        
        var datatypesCmd = new CommandDefinition(
            $"DESCRIBE TABLE {schema}.{table};", cancellationToken: cancellationToken);
        var datatypes = await connection.QueryAsync(datatypesCmd);
        
        var results = datatypes
            .Cast<IDictionary<string, object?>>()
            .Select(d =>
            {
                var columnName = (string)d["name"]!;
                var ordinal = ordinalMap[columnName];
                return new
                {
                    ColumnName = columnName,
                    DataType = (string)d["type"]!,
                    IsNullable = string.Equals((string)d["null?"]!, "Y", StringComparison.OrdinalIgnoreCase),
                    Ordinal = ordinal
                };
            })
            .Select(r => new FullColumnMetadata
            {
                ColumnName = r.ColumnName,
                DataType = r.DataType,
                IsNullable = r.IsNullable,
                Ordinal = r.Ordinal,
                IncludeInStagingTable = true,
                StagingTableExpression = null,
                TargetTableExpression = null
            })
            .ToArray();

        return results;
    }
}