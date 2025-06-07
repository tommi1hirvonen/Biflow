namespace Biflow.Core.Entities;

public class DatabricksFolder(string? name, DatabricksFolder? parent)
{
    private readonly List<DatabricksFile> _files = [];
    private readonly List<DatabricksFolder> _folders = [];
    private readonly DatabricksFolder? _parent = parent;

    public string? Name { get; } = name;

    public int Depth => _parent is null ? 0 : _parent.Depth + 1;

    public IEnumerable<DatabricksFile> Files => _files;

    public IEnumerable<DatabricksFolder> Folders => _folders;

    public static DatabricksFolder FromFiles(IEnumerable<DatabricksFile> files)
    {
        var root = new DatabricksFolder(null, null);

        foreach (var file in files)
        {
            AddFileToFolder(root, file);
        }

        return root;
    }

    private static void AddFileToFolder(DatabricksFolder folder, DatabricksFile file)
    {
        if (file.Folder is null)
        {
            folder._files.Add(file);
            return;
        }

        var pathComponents = file.Folder.Split('/');

        foreach (var component in pathComponents)
        {
            var subfolder = folder._folders.FirstOrDefault(x => x.Name == component);
            if (subfolder is null)
            {
                subfolder = new DatabricksFolder(component, folder);
                folder._folders.Add(subfolder);
            }

            folder = subfolder;
        }

        folder._files.Add(file);
    }
}
