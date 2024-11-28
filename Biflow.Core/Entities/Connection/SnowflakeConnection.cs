using Snowflake.Data.Client;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Text.Json.Serialization;
using Biflow.Core.Entities.Scd;
using Biflow.Core.Entities.Scd.Snowflake;

namespace Biflow.Core.Entities;

public class SnowflakeConnection() : ConnectionBase(ConnectionType.Snowflake)
{
    [Range(0, int.MaxValue)]
    public int MaxConcurrentSqlSteps { get; set; }

    [JsonIgnore]
    public override IEnumerable<Step> Steps => SqlSteps;

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SnowflakeDbConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
    }
    
    public override IColumnMetadataProvider CreateColumnMetadataProvider() =>
        new SnowflakeColumnMetadataProvider(ConnectionString);
    
    public override IScdProvider CreateScdProvider(ScdTable table) =>
        new SnowflakeScdProvider(table, CreateColumnMetadataProvider());
    
    public override DbConnection CreateDbConnection() => new SnowflakeDbConnection(ConnectionString);
}
