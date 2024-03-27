namespace Biflow.Core.Entities;

public class ExeStepExecutionParameter : StepExecutionParameterBase
{
    public ExeStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Exe)
    {

    }

    public ExeStepExecutionParameter(ExeStepParameter parameter, ExeStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public ExeStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
