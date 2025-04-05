namespace Biflow.Executor.Core.FilesExplorer;

public static class FileExplorer
{
    public static DirectoryItem[] GetDirectoryItems(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            var drives = DriveInfo
                .GetDrives()
                .OrderBy(x => x.Name)
                .Select(x => new DirectoryItem(x.Name, x.Name, DirectoryItemType.Drive))
                .ToArray();
            return drives;
        }
        
        var dirInfo = new DirectoryInfo(path);
        var subDirs = dirInfo
            .GetDirectories()
            .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden))
            .OrderBy(x => x.Name)
            .Select(x => new DirectoryItem(x.Name, x.FullName, DirectoryItemType.Directory))
            .ToArray();
        
        var files = dirInfo
            .GetFiles()
            .Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden))
            .OrderBy(x => x.Name)
            .Select(x => new DirectoryItem(x.Name, x.FullName, DirectoryItemType.File))
            .ToArray();
        
        DirectoryItem[] items = [..subDirs, ..files];
        return items;
    }
}