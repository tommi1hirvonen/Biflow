namespace Biflow.DataAccess.Models;

public class DatasetStepExecutionAttempt : StepExecutionAttempt
{
    public DatasetStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Dataset)
    {
    }

    public DatasetStepExecutionAttempt(DatasetStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public DatasetStepExecutionAttempt(DatasetStepExecution execution) : base(execution) { }
}
