using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class EnvironmentVersion
{
    public int VersionId { get; init; }

    public string? Description { get; init; }

    public required string Snapshot { get; init; }

    public required string SnapshotWithReferencesPreserved { get; init; }

    public required DateTimeOffset CreatedOn { get; init; }

    [MaxLength(250)]
    public string? CreatedBy { get; init; }
}
