﻿using Snowflake.Data.Client;
using System.Data.Common;
using System.Text.Json.Serialization;
using Biflow.Core.Entities.Scd;
using Biflow.Core.Entities.Scd.Snowflake;

namespace Biflow.Core.Entities;

public class SnowflakeConnection() : SqlConnectionBase(SqlConnectionType.Snowflake)
{
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
