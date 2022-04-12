namespace Biflow.Ui.Core;

public class CatalogProject
{
    public CatalogProject(long projectId, string projectName)
    {
        ProjectId = projectId;
        ProjectName = projectName;
    }
    public long ProjectId { get; }
    public string ProjectName { get; }
    public Dictionary<long, CatalogPackage> Packages { get; } = new();
}
