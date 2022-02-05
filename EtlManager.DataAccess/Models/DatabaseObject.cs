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

    [Required]
    [Range(0, 100)]
    public int MaxConcurrentWrites { get; set; } = 1;

    [NotMapped] public DatabaseObjectMappingResult SourceMappingResult { get; set; } = new();
    [NotMapped] public DatabaseObjectMappingResult TargetMappingResult { get; set; } = new();

    public IList<Step> Targets { get; set; } = null!;

    public IList<Step> Sources { get; set; } = null!;

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
        else if (obj is ValueTuple<string, string, string, string, bool> vt2)
        {
            return ServerName.Equals(vt2.Item1)
                && DatabaseName.Equals(vt2.Item2)
                && SchemaName.Equals(vt2.Item3)
                && ObjectName.Equals(vt2.Item4);
        }
        else if (obj is Tuple<string, string, string, string, bool> tuple2)
        {
            return ServerName.Equals(tuple2.Item1)
                && DatabaseName.Equals(tuple2.Item2)
                && SchemaName.Equals(tuple2.Item3)
                && ObjectName.Equals(tuple2.Item4);
        }

        return false;
    }

    public override int GetHashCode() => HashCode.Combine(ServerName, DatabaseName, SchemaName, ObjectName);
}

public class DatabaseObjectMappingResult
{
    public bool IsNewAddition { get; set; } = false;

    public bool IsUnreliableMapping { get; set; } = false;

    public bool IsCandidateForRemoval { get; set; } = false;
}