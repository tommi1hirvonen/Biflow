namespace Biflow.DataAccess.Models;

public class EmailStepExecutionParameter : StepExecutionParameterBase
{
    public EmailStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Email, parameterValueType)
    {

    }

    public EmailStepExecutionParameter(EmailStepParameter parameter, EmailStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public EmailStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
