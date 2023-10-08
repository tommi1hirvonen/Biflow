namespace Biflow.DataAccess;

public class PipelineInfo(string name, Dictionary<string, (string Type, object? DefaultValue)> parameters)
{
    public string Name { get; } = name;

    public Dictionary<string, (string Type, object? DefaultValue)> Parameters { get; } = parameters;
}
