using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class EnvironmentVersion
{
    public int VersionId { get; private set; }

    public string? Description { get; set; }

    public required string Snapshot { get; init; }

    public required DateTimeOffset CreatedOn { get; init; }

    [MaxLength(250)]
    public string? CreatedBy { get; init; }
}
