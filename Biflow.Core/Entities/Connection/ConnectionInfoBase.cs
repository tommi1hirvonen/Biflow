using Biflow.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(SqlConnectionInfo), nameof(ConnectionType.Sql))]
[JsonDerivedType(typeof(AnalysisServicesConnectionInfo), nameof(ConnectionType.AnalysisServices))]
public abstract class ConnectionInfoBase(ConnectionType connectionType) : IComparable
{
    [Display(Name = "Connection id")]
    [JsonInclude]
    public Guid ConnectionId { get; private set; }

    [Display(Name = "Connection type")]
    public ConnectionType ConnectionType { get; } = connectionType;

    [Required]
    [MaxLength(250)]
    [Display(Name = "Connection name")]
    public string ConnectionName { get; set; } = "";

    [Required]
    [Display(Name = "Connection string")]
    [JsonSensitive(WhenContains = "password")]
    public string ConnectionString { get; set; } = "";

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        ConnectionInfoBase connection => -connection.ConnectionName.CompareTo(ConnectionName),
        _ => throw new ArgumentException("Object does not inherit from ConnectionInfoBase")
    };

    [JsonIgnore]
    public abstract IEnumerable<Step> Steps { get; }

}
