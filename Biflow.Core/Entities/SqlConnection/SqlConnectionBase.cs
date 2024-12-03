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
    [Display(Name = "Connection id")]
    [JsonInclude]
    public Guid ConnectionId { get; private set; }

    [Display(Name = "Connection type")]
    public SqlConnectionType ConnectionType { get; } = connectionType;

    [Required]
    [MaxLength(250)]
    [Display(Name = "Connection name")]
    public string ConnectionName { get; set; } = "";

    [Required]
    [Display(Name = "Connection string")]
    [JsonSensitive(WhenContains = "password")]
    public string ConnectionString { get; set; } = "";
    
    [Range(0, int.MaxValue)]
    public int MaxConcurrentSqlSteps { get; set; }

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        SqlConnectionBase connection => -string.Compare(connection.ConnectionName, ConnectionName, StringComparison.Ordinal),
        _ => throw new ArgumentException("Object does not inherit from ConnectionInfoBase")
    };

    [JsonIgnore]
    public IEnumerable<SqlStep> SqlSteps { get; set; } = new List<SqlStep>();

    [JsonIgnore]
    public virtual IEnumerable<Step> Steps => SqlSteps;
    
    [JsonIgnore]
    public IEnumerable<ScdTable> ScdTables { get; set; } = new List<ScdTable>();
    
    public abstract IColumnMetadataProvider CreateColumnMetadataProvider();

    public abstract IScdProvider CreateScdProvider(ScdTable table);

    public abstract DbConnection CreateDbConnection();
}
