using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class DatabaseObject
{
    [Key]
    public Guid DatabaseObjectId { get; set; }

    [MaxLength(128)]
    public string ServerName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string DatabaseName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string SchemaName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string ObjectName { get; set; } = string.Empty;

    public ICollection<Step> Targets { get; set; } = null!;

    public ICollection<Step> Sources { get; set; } = null!;
}
