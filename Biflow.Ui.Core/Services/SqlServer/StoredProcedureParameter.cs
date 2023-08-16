namespace Biflow.Ui.Core;

public class StoredProcedureParameter
{
    public StoredProcedureParameter(int parameterId, string parameterName, string parameterType)
    {
        ParameterId = parameterId;
        ParameterName = parameterName; 
        ParameterType = parameterType;
    }
    
    public int ParameterId { get; }
    
    public string ParameterName { get; }
    
    public string ParameterType { get; }
}
