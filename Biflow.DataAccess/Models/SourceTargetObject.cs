using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class SourceTargetObject
{
    [Key]
    public Guid ObjectId { get; set; }

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

    [NotMapped] public SourceTargetMappingResult SourceMappingResult { get; set; } = new();
    [NotMapped] public SourceTargetMappingResult TargetMappingResult { get; set; } = new();

    public IList<Step> Targets { get; set; } = null!;

    public IList<Step> Sources { get; set; } = null!;

    public override bool Equals(object? obj)
    {
        if (obj is SourceTargetObject dbo)
        {
            return ServerName.EqualsIgnoreCase(dbo.ServerName)
                && DatabaseName.EqualsIgnoreCase(dbo.DatabaseName)
                && SchemaName.EqualsIgnoreCase(dbo.SchemaName)
                && ObjectName.EqualsIgnoreCase(dbo.ObjectName);
        }
        else if (obj is ValueTuple<string, string, string, string> vt)
        {
            return ServerName.EqualsIgnoreCase(vt.Item1)
                && DatabaseName.EqualsIgnoreCase(vt.Item2)
                && SchemaName.EqualsIgnoreCase(vt.Item3)
                && ObjectName.EqualsIgnoreCase(vt.Item4);
        }
        else if (obj is Tuple<string, string, string, string> tuple)
        {
            return ServerName.EqualsIgnoreCase(tuple.Item1)
                && DatabaseName.EqualsIgnoreCase(tuple.Item2)
                && SchemaName.EqualsIgnoreCase(tuple.Item3)
                && ObjectName.EqualsIgnoreCase(tuple.Item4);
        }
        else if (obj is ValueTuple<string, string, string, string, bool> vt2)
        {
            return ServerName.EqualsIgnoreCase(vt2.Item1)
                && DatabaseName.EqualsIgnoreCase(vt2.Item2)
                && SchemaName.EqualsIgnoreCase(vt2.Item3)
                && ObjectName.EqualsIgnoreCase(vt2.Item4);
        }
        else if (obj is Tuple<string, string, string, string, bool> tuple2)
        {
            return ServerName.EqualsIgnoreCase(tuple2.Item1)
                && DatabaseName.EqualsIgnoreCase(tuple2.Item2)
                && SchemaName.EqualsIgnoreCase(tuple2.Item3)
                && ObjectName.EqualsIgnoreCase(tuple2.Item4);
        }

        return false;
    }

    public override int GetHashCode() =>
        HashCode.Combine(
            ServerName.ToLower(),
            DatabaseName.ToLower(),
            SchemaName.ToLower(),
            ObjectName.ToLower());
}

public class SourceTargetMappingResult
{
    public bool IsNewAddition { get; set; } = false;

    public bool IsUnreliableMapping { get; set; } = false;

    public bool IsCandidateForRemoval { get; set; } = false;
}