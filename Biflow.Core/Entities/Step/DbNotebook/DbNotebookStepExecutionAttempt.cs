namespace Biflow.Core.Entities;

public class DbNotebookStepExecutionAttempt : StepExecutionAttempt
{

    public DbNotebookStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.DatabricksNotebook)
    {
    }

    public DbNotebookStepExecutionAttempt(DbNotebookStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public DbNotebookStepExecutionAttempt(DbNotebookStepExecution execution) : base(execution) { }

    public long? JobRunId { get; set; }
}
