namespace Biflow.DataAccess.Models;

public class PackageStepExecutionAttempt : StepExecutionAttempt
{

    public PackageStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Package)
    {
    }

    public PackageStepExecutionAttempt(PackageStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public PackageStepExecutionAttempt(PackageStepExecution execution) : base(execution) { }

    public long? PackageOperationId { get; set; }
}
