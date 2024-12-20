namespace Biflow.Core.Entities;

public class FabricStepExecutionParameter : StepExecutionParameterBase
{
    public FabricStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Fabric)
    {
        
    }

    public FabricStepExecutionParameter(FabricStepParameter parameter, FabricStepExecution execution)
        : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public FabricStepExecution StepExecution { get; init; } = null!;
    
    public override StepExecution BaseStepExecution => StepExecution;
}