namespace Biflow.Core.Entities;

public class DatabricksStepExecutionParameter : StepExecutionParameterBase
{
    public DatabricksStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.DatabricksNotebook)
    {

    }

    public DatabricksStepExecutionParameter(DatabricksStepParameter parameter, DatabricksStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public DatabricksStepExecution StepExecution { get; init; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}