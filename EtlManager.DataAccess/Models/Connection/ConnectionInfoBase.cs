using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtlManager.DataAccess.Models;

public abstract class ConnectionInfoBase : IComparable
{
    public ConnectionInfoBase(ConnectionType connectionType, string connectionName, string connectionString)
    {
        ConnectionName = connectionName;
        ConnectionString = connectionString;
        ConnectionType = connectionType;
    }

    [Key]
    [Display(Name = "Connection id")]
    public Guid ConnectionId { get; set; }

    [Required]
    [Display(Name = "Connection type")]
    public ConnectionType ConnectionType { get; }

    [Required]
    [MaxLength(250)]
    [Display(Name = "Connection name")]
    public string ConnectionName { get; set; }

    [Required]
    [Display(Name = "Connection string")]
    public string ConnectionString { get; set; }

    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        ConnectionInfoBase connection => -connection.ConnectionName.CompareTo(ConnectionName),
        _ => throw new ArgumentException("Object does not inherit from ConnectionInfoBase")
    };

    [NotMapped]
    public abstract IEnumerable<Step> Steps { get; }

}
