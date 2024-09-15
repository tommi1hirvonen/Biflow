namespace Biflow.Ui.SqlMetadataExtensions;

public class CatalogFolder(long folderId, string folderName)
{
    public long FolderId { get; } = folderId;

    public string FolderName { get; } = folderName;

    public Dictionary<long, CatalogProject> Projects { get; } = [];
}
