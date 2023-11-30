using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

namespace Biflow.DataAccess.Models;

[Table("DataObject")]
public class DataObject : IDataObject
{
    [Key]
    public Guid ObjectId { get; private set; }

    [Required]
    [Uri]
    [MaxLength(500)]
    public string ObjectUri { get; set; } = string.Empty;

    [Required]
    [Range(0, 100)]
    public int MaxConcurrentWrites { get; set; } = 1;

    [NotMapped] public DataObjectMappingResult SourceMappingResult { get; set; } = new();
    [NotMapped] public DataObjectMappingResult TargetMappingResult { get; set; } = new();

    public IList<StepTarget> Writers { get; set; } = null!;

    public IList<StepSource> Readers { get; set; } = null!;

    public bool UriEquals(IDataObject? other) =>
        other is not null &&
        ObjectUri.EqualsIgnoreCase(other.ObjectUri);

    public static string CreateTableUri(string server, string database, string schema, string table)
    {
        server = Uri.EscapeDataString(server);
        database = Uri.EscapeDataString(database);
        schema = Uri.EscapeDataString(schema);
        table = Uri.EscapeDataString(table);
        return $"table://{server}/{database}/{schema}/{table}";
    }
}

public class DataObjectMappingResult
{
    public bool IsNewAddition { get; set; } = false;

    public bool IsUnreliableMapping { get; set; } = false;

    public bool IsCandidateForRemoval { get; set; } = false;
}

public interface IDataObject
{
    public string ObjectUri { get; }
}