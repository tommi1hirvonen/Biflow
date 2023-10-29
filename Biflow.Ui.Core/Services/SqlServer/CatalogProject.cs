namespace Biflow.Ui.Core;

public class CatalogProject(long projectId, string projectName)
{
    public long ProjectId { get; } = projectId;

    public string ProjectName { get; } = projectName;

    public Dictionary<long, CatalogPackage> Packages { get; } = [];
}
