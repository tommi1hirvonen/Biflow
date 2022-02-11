namespace Biflow.DataAccess.Models;

public record SqlStepExecutionAttempt : StepExecutionAttempt
{
    public SqlStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Sql)
    {
    }
}
