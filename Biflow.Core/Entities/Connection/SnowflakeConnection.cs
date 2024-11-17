using Snowflake.Data.Client;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class SnowflakeConnection() : ConnectionBase(ConnectionType.Snowflake)
{
    [Range(0, int.MaxValue)]
    public int MaxConcurrentSqlSteps { get; set; } = 0;

    [JsonIgnore]
    public override IEnumerable<Step> Steps => SqlSteps;

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SnowflakeDbConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
    }
}
