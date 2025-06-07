namespace Biflow.Core.Entities;

public class DatabricksStepExecutionAttempt : StepExecutionAttempt
{

    public DatabricksStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Databricks)
    {
    }

    public DatabricksStepExecutionAttempt(DatabricksStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public DatabricksStepExecutionAttempt(DatabricksStepExecution execution) : base(execution) { }

    public long? JobRunId { get; set; }
}
