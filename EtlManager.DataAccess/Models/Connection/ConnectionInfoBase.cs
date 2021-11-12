using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManager.DataAccess.Models;

public abstract class ConnectionInfoBase
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

}
