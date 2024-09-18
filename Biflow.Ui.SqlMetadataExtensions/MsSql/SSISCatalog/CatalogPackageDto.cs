namespace Biflow.Ui.SqlMetadataExtensions;

internal class CatalogPackageDto(long packageId, string packageName)
{
    public long PackageId { get; } = packageId;

    public string PackageName { get; } = packageName;
}