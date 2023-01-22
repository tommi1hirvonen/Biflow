namespace Biflow.DataAccess.Models;

public class PackageStepExecutionParameter : StepExecutionParameterBase
{
    public PackageStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Package, parameterValueType)
    {

    }

    public PackageStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;

    public ParameterLevel ParameterLevel { get; set; }

    public override string DisplayName => $"${ParameterLevel}::{ParameterName}";
}
