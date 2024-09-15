namespace Biflow.Ui.SqlMetadataExtensions;

internal class CatalogProjectDto(long projectId, string projectName)
{
    public long ProjectId { get; } = projectId;

    public string ProjectName { get; } = projectName;
}