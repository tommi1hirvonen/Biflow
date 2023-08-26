namespace Biflow.DataAccess.Models;

public class SqlStepExecutionAttempt : StepExecutionAttempt
{
    public SqlStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Sql)
    {
    }

    protected SqlStepExecutionAttempt(SqlStepExecutionAttempt other) : base(other)
    {
    }

    public SqlStepExecutionAttempt(SqlStepExecution execution) : base(execution)
    {
    }

    protected override StepExecutionAttempt Clone() => new SqlStepExecutionAttempt(this);
}
