using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("Connection")]
[JsonDerivedType(typeof(SqlConnectionInfo), nameof(ConnectionType.Sql))]
[JsonDerivedType(typeof(AnalysisServicesConnectionInfo), nameof(ConnectionType.AnalysisServices))]
public abstract class ConnectionInfoBase(ConnectionType connectionType) : IComparable
{
    [Key]
    [Display(Name = "Connection id")]
    [JsonInclude]
    public Guid ConnectionId { get; private set; }

    [Required]
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

    [NotMapped]
    [JsonIgnore]
    public abstract IEnumerable<Step> Steps { get; }

}
