namespace Biflow.Core.Entities;

public class FunctionStepExecutionParameter : StepExecutionParameterBase
{
    public FunctionStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Function)
    {

    }

    public FunctionStepExecutionParameter(FunctionStepParameter parameter, FunctionStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public FunctionStepExecution StepExecution { get; init; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
