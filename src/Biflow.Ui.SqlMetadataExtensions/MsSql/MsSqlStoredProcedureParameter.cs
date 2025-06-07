using JetBrains.Annotations;

namespace Biflow.Ui.SqlMetadataExtensions;

[UsedImplicitly]
public class MsSqlStoredProcedureParameter(int parameterId, string parameterName, string parameterType)
    : IStoredProcedureParameter
{
    public int ParameterId { get; } = parameterId;

    public string ParameterName { get; } = parameterName;

    public string ParameterType { get; } = parameterType;
}