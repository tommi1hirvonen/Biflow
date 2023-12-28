namespace Biflow.Ui.SqlServer;

internal class CatalogProjectDto(long projectId, string projectName)
{
    public long ProjectId { get; } = projectId;

    public string ProjectName { get; } = projectName;
}