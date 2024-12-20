namespace Biflow.Core.Entities;

public class EmailStepExecutionParameter : StepExecutionParameterBase
{
    public EmailStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Email)
    {

    }

    public EmailStepExecutionParameter(EmailStepParameter parameter, EmailStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
    }

    public EmailStepExecution StepExecution { get; init; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;
}
