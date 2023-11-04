namespace Biflow.DataAccess.Models;

public class AgentJobStepExecutionAttempt : StepExecutionAttempt
{
    public AgentJobStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.AgentJob)
    {
    }

    public AgentJobStepExecutionAttempt(AgentJobStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public AgentJobStepExecutionAttempt(AgentJobStepExecution execution) : base(execution) { }
}
