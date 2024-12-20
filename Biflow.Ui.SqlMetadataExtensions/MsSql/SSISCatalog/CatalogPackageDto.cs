using JetBrains.Annotations;

namespace Biflow.Ui.SqlMetadataExtensions;

[UsedImplicitly]
internal class CatalogPackageDto(long packageId, string packageName)
{
    public long PackageId { get; } = packageId;

    public string PackageName { get; } = packageName;
}