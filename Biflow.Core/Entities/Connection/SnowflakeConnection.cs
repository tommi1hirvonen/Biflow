using Snowflake.Data.Client;

namespace Biflow.Core.Entities;

public class SnowflakeConnection() : ConnectionBase(ConnectionType.Snowflake)
{
    public override IEnumerable<Step> Steps => [];

    public override async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SnowflakeDbConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
    }
}
