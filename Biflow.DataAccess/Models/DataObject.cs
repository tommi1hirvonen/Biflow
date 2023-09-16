using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("DataObject")]
public class DataObject : IDataObject
{
    [Key]
    public Guid ObjectId { get; private set; }

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

    [NotMapped] public DataObjectMappingResult SourceMappingResult { get; set; } = new();
    [NotMapped] public DataObjectMappingResult TargetMappingResult { get; set; } = new();

    public IList<Step> Writers { get; set; } = null!;

    public IList<Step> Readers { get; set; } = null!;

    public bool NamesEqual(IDataObject other) =>
        other is not null &&
        ServerName.EqualsIgnoreCase(other.ServerName)
        && DatabaseName.EqualsIgnoreCase(other.DatabaseName)
        && SchemaName.EqualsIgnoreCase(other.SchemaName)
        && ObjectName.EqualsIgnoreCase(other.ObjectName);
}

public class DataObjectMappingResult
{
    public bool IsNewAddition { get; set; } = false;

    public bool IsUnreliableMapping { get; set; } = false;

    public bool IsCandidateForRemoval { get; set; } = false;
}

public interface IDataObject
{
    public string ServerName { get; }
    public string DatabaseName { get; }
    public string SchemaName { get; }
    public string ObjectName { get; }
}