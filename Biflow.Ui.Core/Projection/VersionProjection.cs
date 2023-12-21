namespace Biflow.Ui.Core.Projection;

public record VersionProjection(int VersionId, string? Description, DateTimeOffset CreatedDateTime, string? CreatedBy);