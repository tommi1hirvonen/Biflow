namespace Biflow.Ui.SqlServer;

public class CatalogPackage(long packageId, string packageName, CatalogProject project)
{
    public long PackageId { get; } = packageId;

    public string PackageName { get; } = packageName;

    public Dictionary<long, CatalogParameter> Parameters { get; } = [];

    public CatalogProject Project { get; } = project;
}
