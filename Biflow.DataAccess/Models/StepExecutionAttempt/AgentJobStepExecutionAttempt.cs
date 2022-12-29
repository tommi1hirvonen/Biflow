namespace Biflow.DataAccess.Models;

public class AgentJobStepExecutionAttempt : StepExecutionAttempt
{
    public AgentJobStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.AgentJob)
    {
    }

    protected AgentJobStepExecutionAttempt(AgentJobStepExecutionAttempt other) : base(other)
    {
    }

    public override StepExecutionAttempt Clone() => new AgentJobStepExecutionAttempt(this);
}
