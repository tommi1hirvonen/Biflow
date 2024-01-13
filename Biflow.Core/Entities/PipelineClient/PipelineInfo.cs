namespace Biflow.Core.Entities;

public class PipelineInfo(string name, string? folder, Dictionary<string, (string Type, string? DefaultValue)> parameters)
{
    public string Name { get; } = name;

    public string? Folder { get; } = folder;

    public Dictionary<string, (string Type, string? DefaultValue)> Parameters { get; } = parameters;
}
