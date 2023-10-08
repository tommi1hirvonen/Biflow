using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Connection")]
public abstract class ConnectionInfoBase(ConnectionType connectionType, string connectionName, string connectionString) : IComparable
{
    [Key]
    [Display(Name = "Connection id")]
    public Guid ConnectionId { get; private set; }

    [Required]
    [Display(Name = "Connection type")]
    public ConnectionType ConnectionType { get; } = connectionType;

    [Required]
    [MaxLength(250)]
    [Display(Name = "Connection name")]
    public string ConnectionName { get; set; } = connectionName;

    [Required]
    [Display(Name = "Connection string")]
    public string ConnectionString { get; set; } = connectionString;

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        ConnectionInfoBase connection => -connection.ConnectionName.CompareTo(ConnectionName),
        _ => throw new ArgumentException("Object does not inherit from ConnectionInfoBase")
    };

    [NotMapped]
    public abstract IEnumerable<Step> Steps { get; }

}
