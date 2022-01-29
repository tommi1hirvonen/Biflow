using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    [NotMapped]
    public bool IsCandidateForRemoval { get; set; } = false;

    [NotMapped]
    public bool IsNewAddition { get; set; } = false;

    public ICollection<Step> Targets { get; set; } = null!;

    public ICollection<Step> Sources { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is DatabaseObject dbo)
        {
            return ServerName.Equals(dbo.ServerName)
                && DatabaseName.Equals(dbo.DatabaseName)
                && SchemaName.Equals(dbo.SchemaName)
                && ObjectName.Equals(dbo.ObjectName);
        }
        else if (obj is ValueTuple<string, string, string, string> vt)
        {
            return ServerName.Equals(vt.Item1)
                && DatabaseName.Equals(vt.Item2)
                && SchemaName.Equals(vt.Item3)
                && ObjectName.Equals(vt.Item4);
        }
        else if (obj is Tuple<string, string, string, string> tuple)
        {
            return ServerName.Equals(tuple.Item1)
                && DatabaseName.Equals(tuple.Item2)
                && SchemaName.Equals(tuple.Item3)
                && ObjectName.Equals(tuple.Item4);
        }

        return false;
    }

    public override int GetHashCode() => HashCode.Combine(ServerName, DatabaseName, SchemaName, ObjectName);
}
