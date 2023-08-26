namespace Biflow.DataAccess.Models;

public class ExeStepExecutionParameter : StepExecutionParameterBase
{
    public ExeStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Exe, parameterValueType)
    {

    }

    public ExeStepExecutionParameter(ExeStepParameter parameter, ExeStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public ExeStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
