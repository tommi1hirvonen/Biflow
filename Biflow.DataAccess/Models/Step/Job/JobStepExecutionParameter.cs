namespace Biflow.DataAccess.Models;

public class JobStepExecutionParameter : StepExecutionParameterBase
{
    public JobStepExecutionParameter(string parameterName, object parameterValue, ParameterValueType parameterValueType)
        : base(parameterName, parameterValue, ParameterType.Job, parameterValueType)
    {

    }

    public JobStepExecutionParameter(JobStepParameter parameter, JobStepExecution execution) : base(parameter, execution)
    {
        StepExecution = execution;
        AssignToJobParameterId = parameter.AssignToJobParameterId;
    }

    public JobStepExecution StepExecution { get; set; } = null!;

    public override StepExecution BaseStepExecution => StepExecution;

    public Guid AssignToJobParameterId { get; set; }
}
