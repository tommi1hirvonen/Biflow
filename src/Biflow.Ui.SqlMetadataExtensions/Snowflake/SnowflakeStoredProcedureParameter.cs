using JetBrains.Annotations;

namespace Biflow.Ui.SqlMetadataExtensions;

[UsedImplicitly]
public class SnowflakeStoredProcedureParameter(string parameterName, string parameterType) : IStoredProcedureParameter
{
    public string ParameterName { get; } = parameterName;

    public string ParameterType { get; } = parameterType;

    public override bool Equals(object? obj)
    {
        if (obj is SnowflakeStoredProcedureParameter other)
        {
            return ParameterName == other.ParameterName && ParameterType == other.ParameterType;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ParameterName, ParameterType);
    }
}
