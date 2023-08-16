namespace Biflow.Ui.Core;

public class CatalogPackage
{
    public CatalogPackage(long packageId, string packageName)
    {
        PackageId = packageId;
        PackageName = packageName;
    }

    public long PackageId { get; }
    
    public string PackageName { get; }
    
    public Dictionary<long, CatalogParameter> Parameters { get; } = new();
}
