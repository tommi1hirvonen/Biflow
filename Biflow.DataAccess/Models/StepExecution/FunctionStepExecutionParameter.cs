namespace Biflow.DataAccess.Models;

public class FunctionStepExecutionParameter : StepExecutionParameterBase
{
    public FunctionStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Function, parameterValueType)
    {

    }

    public FunctionStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
