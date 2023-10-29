namespace Biflow.Ui.Core;

public class CatalogPackage(long packageId, string packageName)
{
    public long PackageId { get; } = packageId;

    public string PackageName { get; } = packageName;

    public Dictionary<long, CatalogParameter> Parameters { get; } = [];
}
