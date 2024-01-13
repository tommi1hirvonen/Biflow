namespace Biflow.Core.Entities;

public class PackageStepExecutionParameter : StepExecutionParameterBase
{
    public PackageStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Package, parameterValueType)
    {

    }

    public PackageStepExecutionParameter(PackageStepParameter parameter, PackageStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
        ParameterLevel = parameter.ParameterLevel;
    }

    public PackageStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;

    public ParameterLevel ParameterLevel { get; set; }

    public override string DisplayName => $"${ParameterLevel}::{ParameterName}";
}
