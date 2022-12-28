namespace Biflow.DataAccess.Models;

public record PackageStepExecutionAttempt : StepExecutionAttempt
{

    public PackageStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Package)
    {
    }

    [IncludeInReset]
    public long? PackageOperationId { get; set; }
}
