using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("EnvironmentVersion")]
public class EnvironmentVersion
{
    [Key]
    public int VersionId { get; private set; }

    public string? Description { get; set; }

    public required string Snapshot { get; init; }

    public required DateTimeOffset CreatedDateTime { get; init; }

    public string? CreatedBy { get; init; }
}
