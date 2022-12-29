namespace Biflow.DataAccess.Models;

public class DatasetStepExecutionAttempt : StepExecutionAttempt
{
    public DatasetStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Dataset)
    {
    }

    protected DatasetStepExecutionAttempt(DatasetStepExecutionAttempt other) : base(other)
    {
    }

    public override StepExecutionAttempt Clone() => new DatasetStepExecutionAttempt(this);
}
