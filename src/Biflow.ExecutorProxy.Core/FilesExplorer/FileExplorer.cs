namespace Biflow.ExecutorProxy.Core.FilesExplorer;

public static class FileExplorer
{
    public static DirectoryItem[] GetDirectoryItems(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            var drives = DriveInfo
                .GetDrives()
                .OrderBy(x => x.Name)
                .Select(x => new DirectoryItem(x.Name, x.Name, DirectoryItemType.Drive, false))
                .ToArray();
            return drives;
        }
        
        var dirInfo = new DirectoryInfo(path);
        var subDirs = dirInfo
            .GetDirectories()
            .OrderBy(x => x.Name)
            .Select(x => new DirectoryItem(
                x.Name,
                x.FullName,
                DirectoryItemType.Directory,
                x.Attributes.HasFlag(FileAttributes.Hidden)))
            .ToArray();
        
        var files = dirInfo
            .GetFiles()
            .OrderBy(x => x.Name)
            .Select(x => new DirectoryItem(
                x.Name,
                x.FullName,
                DirectoryItemType.File,
                x.Attributes.HasFlag(FileAttributes.Hidden)))
            .ToArray();
        
        DirectoryItem[] items = [..subDirs, ..files];
        return items;
    }
}