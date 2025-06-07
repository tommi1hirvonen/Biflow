using Biflow.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Text.Json.Serialization;
using Biflow.Core.Entities.Scd;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(MsSqlConnection), nameof(SqlConnectionType.MsSql))]
[JsonDerivedType(typeof(SnowflakeConnection), nameof(SqlConnectionType.Snowflake))]
public abstract class SqlConnectionBase(SqlConnectionType connectionType) : IComparable
{
    public Guid ConnectionId { get; init; }

    public SqlConnectionType ConnectionType { get; } = connectionType;

    [Required]
    [MaxLength(250)]
    public string ConnectionName { get; set; } = "";

    [Required]
    [JsonSensitive(WhenContains = "password")]
    public string ConnectionString { get; set; } = "";
    
    [Range(0, int.MaxValue)]
    public int MaxConcurrentSqlSteps { get; set; }
    
    [MaxLength(128)]
    public string? ScdDefaultTargetSchema { get; set; }

    [MaxLength(128)]
    public string? ScdDefaultTargetTableSuffix { get; set; } = "_SCD";
    
    [MaxLength(128)]
    public string? ScdDefaultStagingSchema { get; set; }
    
    [MaxLength(128)]
    public string? ScdDefaultStagingTableSuffix { get; set; } = "_SCD_DELTA";

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        SqlConnectionBase connection => -string.Compare(connection.ConnectionName, ConnectionName, StringComparison.Ordinal),
        _ => throw new ArgumentException("Object does not inherit from ConnectionInfoBase")
    };

    [JsonIgnore]
    public IEnumerable<SqlStep> SqlSteps { get; init; } = new List<SqlStep>();

    [JsonIgnore]
    public virtual IEnumerable<Step> Steps => SqlSteps;
    
    [JsonIgnore]
    public IEnumerable<ScdTable> ScdTables { get; init; } = new List<ScdTable>();
    
    public abstract IColumnMetadataProvider CreateColumnMetadataProvider();

    public abstract IScdProvider CreateScdProvider(ScdTable table);

    public abstract DbConnection CreateDbConnection();
}
