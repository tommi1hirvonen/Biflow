namespace Biflow.Ui.SqlMetadataExtensions;

public class CatalogParameter(
    long parameterId,
    string parameterName,
    string parameterType,
    object? designDefaultValue,
    object? defaultValue,
    int connectionManagerParameter,
    int projectParameter)
{
    public long ParameterId { get; } = parameterId;

    public string ParameterName { get; } = parameterName;

    public string ParameterType { get; } = parameterType;

    public object? DesignDefaultValue { get; } = designDefaultValue;

    public object? DefaultValue { get; } = defaultValue;

    public bool ConnectionManagerParameter { get; } = connectionManagerParameter > 0;

    public bool ProjectParameter { get; } = projectParameter > 0;
}
