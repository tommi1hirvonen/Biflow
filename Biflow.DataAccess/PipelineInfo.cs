namespace Biflow.DataAccess;

public class PipelineInfo
{
    public PipelineInfo(string name, Dictionary<string, (string Type, object? DefaultValue)> parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    public string Name { get; }

    public Dictionary<string, (string Type, object? DefaultValue)> Parameters { get; }
}
