namespace Biflow.Core.Entities;

public class DatabricksFile(string path, string name, string? folder)
{
    public string Name { get; } = name;

    public string? Folder { get; } = folder;

    public string Path { get; } = path;
}
