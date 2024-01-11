namespace Biflow.DataAccess.Models;

public class SqlStepExecutionAttempt : StepExecutionAttempt
{
    public SqlStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Sql)
    {
    }

    public SqlStepExecutionAttempt(SqlStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    internal SqlStepExecutionAttempt(SqlStepExecution execution) : base(execution)
    {
    }
}
