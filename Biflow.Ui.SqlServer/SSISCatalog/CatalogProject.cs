namespace Biflow.Ui.SqlServer;

public class CatalogProject(long projectId, string projectName, CatalogFolder folder)
{
    public long ProjectId { get; } = projectId;

    public string ProjectName { get; } = projectName;

    public CatalogFolder Folder { get; } = folder;

    public Dictionary<long, CatalogPackage> Packages { get; } = [];
}
