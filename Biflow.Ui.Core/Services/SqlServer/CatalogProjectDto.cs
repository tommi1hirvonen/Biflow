namespace Biflow.Ui.Core;

internal class CatalogProjectDto(long projectId, string projectName)
{
    public long ProjectId { get; } = projectId;

    public string ProjectName { get; } = projectName;
}