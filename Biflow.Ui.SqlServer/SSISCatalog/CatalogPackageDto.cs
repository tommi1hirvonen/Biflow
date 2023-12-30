namespace Biflow.Ui.SqlServer;

internal class CatalogPackageDto(long packageId, string packageName)
{
    public long PackageId { get; } = packageId;

    public string PackageName { get; } = packageName;
}