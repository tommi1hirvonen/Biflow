namespace Biflow.Ui.SqlServer;

public class StoredProcedureParameter(int parameterId, string parameterName, string parameterType)
{
    public int ParameterId { get; } = parameterId;

    public string ParameterName { get; } = parameterName;

    public string ParameterType { get; } = parameterType;
}
