namespace Biflow.DataAccess.Models;

public record PackageStepExecutionAttempt : StepExecutionAttempt
{

    public PackageStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Package)
    {
    }

    public long? PackageOperationId { get; set; }

    protected override void ResetInstanceMembers()
    {
        PackageOperationId = null;
    }
}
