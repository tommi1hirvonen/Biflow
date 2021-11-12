namespace EtlManager.DataAccess.Models;

public class PackageStepExecutionParameter : StepExecutionParameterBase
{
    public PackageStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Package, parameterValueType)
    {

    }

    public ParameterLevel ParameterLevel { get; set; }
}
