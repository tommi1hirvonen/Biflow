namespace Biflow.Core.Entities;

public class VmStepExecutionAttempt : StepExecutionAttempt
{
    public VmStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Vm)
    {
    }

    public VmStepExecutionAttempt(VmStepExecutionAttempt other, int retryAttemptIndex)
        : base(other, retryAttemptIndex)
    {
    }

    public VmStepExecutionAttempt(VmStepExecution execution) : base(execution)
    {
    }
}
