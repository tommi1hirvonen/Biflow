namespace Biflow.Core.Entities;

public class JobStepExecutionParameter : StepExecutionParameterBase
{
    public JobStepExecutionParameter(string parameterName, ParameterValue parameterValue)
        : base(parameterName, parameterValue, ParameterType.Job)
    {

    }

    public JobStepExecutionParameter(JobStepParameter parameter, JobStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
        AssignToJobParameterId = parameter.AssignToJobParameterId;
    }

    public JobStepExecution StepExecution { get; init; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;

    public Guid AssignToJobParameterId { get; init; }
}
