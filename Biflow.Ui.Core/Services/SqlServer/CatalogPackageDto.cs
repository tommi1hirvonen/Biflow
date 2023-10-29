namespace Biflow.Ui.Core;

internal class CatalogPackageDto(long packageId, string packageName)
{
    public long PackageId { get; } = packageId;

    public string PackageName { get; } = packageName;
}