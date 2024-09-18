namespace Biflow.Ui.SqlMetadataExtensions;

public class MsSqlStoredProcedureParameter(int parameterId, string parameterName, string parameterType) : IStoredProcedureParameter
{
    public int ParameterId { get; } = parameterId;

    public string ParameterName { get; } = parameterName;

    public string ParameterType { get; } = parameterType;
}