using JetBrains.Annotations;

namespace Biflow.Ui.SqlMetadataExtensions;

[UsedImplicitly]
internal class CatalogProjectDto(long projectId, string projectName)
{
    public long ProjectId { get; } = projectId;

    public string ProjectName { get; } = projectName;
}