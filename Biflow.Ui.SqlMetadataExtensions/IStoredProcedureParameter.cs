namespace Biflow.Ui.SqlMetadataExtensions;

public interface IStoredProcedureParameter
{
    public string ParameterName { get; }

    public string ParameterType { get; }
}
