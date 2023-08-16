namespace Biflow.Ui.Core;

public class CatalogFolder
{
    public CatalogFolder(long folderId, string folderName)
    {
        FolderId = folderId;
        FolderName = folderName;
    }

    public long FolderId { get; }
    
    public string FolderName { get; }
    
    public Dictionary<long, CatalogProject> Projects { get; } = new();
}
