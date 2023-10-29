namespace Biflow.DataAccess.Models;

public class PipelineInfo(string name, Dictionary<string, (string Type, string? DefaultValue)> parameters)
{
    public string Name { get; } = name;

    public Dictionary<string, (string Type, string? DefaultValue)> Parameters { get; } = parameters;
}
