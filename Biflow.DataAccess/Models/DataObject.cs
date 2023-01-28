using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("DataObject")]
public class DataObject
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

    [NotMapped] public DataObjectMappingResult SourceMappingResult { get; set; } = new();
    [NotMapped] public DataObjectMappingResult TargetMappingResult { get; set; } = new();

    public IList<Step> Writers { get; set; } = null!;

    public IList<Step> Readers { get; set; } = null!;

    public void Deconstruct(out string serverName, out string databaseName, out string schemaName, out string objectName)
    {
        (serverName, databaseName, schemaName, objectName) = (ServerName, DatabaseName, SchemaName, ObjectName);
    }

    public bool NamesEqual((string ServerName, string DatabaseName, string SchemaName, string ObjectName) other) =>
        ServerName.EqualsIgnoreCase(other.ServerName)
        && DatabaseName.EqualsIgnoreCase(other.DatabaseName)
        && SchemaName.EqualsIgnoreCase(other.SchemaName)
        && ObjectName.EqualsIgnoreCase(other.ObjectName);

    public bool NamesEqual(DataObject other) =>
        NamesEqual((other.ServerName, other.DatabaseName, other.SchemaName, other.ObjectName));

    public bool NamesEqual((string ServerName, string DatabaseName, string SchemaName, string ObjectName, object? _) other) =>
        NamesEqual((other.ServerName, other.DatabaseName, other.SchemaName, other.ObjectName));

}

public class DataObjectMappingResult
{
    public bool IsNewAddition { get; set; } = false;

    public bool IsUnreliableMapping { get; set; } = false;

    public bool IsCandidateForRemoval { get; set; } = false;
}