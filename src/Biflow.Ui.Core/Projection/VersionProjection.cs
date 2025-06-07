namespace Biflow.Ui.Core.Projection;

public record VersionProjection(int VersionId, string? Description, DateTimeOffset CreatedOn, string? CreatedBy);