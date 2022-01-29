using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class DatabaseObject
{
    [Key]
    public Guid DatabaseObjectId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string ServerName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string SchemaName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string ObjectName { get; set; } = string.Empty;

    public ICollection<Step> Targets { get; set; } = null!;

    public ICollection<Step> Sources { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        var dbo = obj as DatabaseObject;
        return ServerName.Equals(dbo?.ServerName)
            && DatabaseName.Equals(dbo.DatabaseName)
            && SchemaName.Equals(dbo.SchemaName)
            && ObjectName.Equals(dbo.ObjectName);
    }

    public override int GetHashCode() => HashCode.Combine(ServerName, DatabaseName, SchemaName, ObjectName);
}
