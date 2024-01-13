namespace Biflow.Core.Entities;

public class FunctionStepExecutionParameter : StepExecutionParameterBase
{
    public FunctionStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Function, parameterValueType)
    {

    }

    public FunctionStepExecutionParameter(FunctionStepParameter parameter, FunctionStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public FunctionStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
