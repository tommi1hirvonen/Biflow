using Snowflake.Data.Client;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class SnowflakeConnection() : ConnectionBase(ConnectionType.Snowflake)
{
    [JsonIgnore]
    public override IEnumerable<Step> Steps => SqlSteps;

    public override async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SnowflakeDbConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
    }
}
