namespace EtlManager.DataAccess.Models;

public class StepExecutionParameter : StepExecutionParameterBase
{
    public StepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Base, parameterValueType)
    {

    }
}
