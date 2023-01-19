namespace Biflow.DataAccess.Models;

public class ExeStepExecutionParameter : StepExecutionParameterBase
{
    public ExeStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Exe, parameterValueType)
    {

    }

    public ExeStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
