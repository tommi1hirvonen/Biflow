namespace Biflow.Ui.Core;

public class CatalogParameter
{
    public CatalogParameter(
        long parameterId,
        string parameterName,
        string parameterType,
        object? designDefaultValue,
        object? defaultValue,
        int connectionManagerParameter,
        int projectParameter)
    {
        ParameterId = parameterId;
        ParameterName = parameterName;
        ParameterType = parameterType;
        DesignDefaultValue = designDefaultValue;
        DefaultValue = defaultValue;
        ConnectionManagerParameter = connectionManagerParameter > 0;
        ProjectParameter = projectParameter > 0;
    }
    public long ParameterId { get; }
    public string ParameterName { get; }
    public string ParameterType { get; }
    public object? DesignDefaultValue { get; }
    public object? DefaultValue { get; }
    public bool ConnectionManagerParameter { get; }
    public bool ProjectParameter { get; }
}
