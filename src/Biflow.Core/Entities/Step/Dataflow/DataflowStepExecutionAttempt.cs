namespace Biflow.Core.Entities;

public class DataflowStepExecutionAttempt : StepExecutionAttempt
{
    public DataflowStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Dataflow)
    {
    }

    public DataflowStepExecutionAttempt(DataflowStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public DataflowStepExecutionAttempt(DataflowStepExecution execution) : base(execution) { }
}