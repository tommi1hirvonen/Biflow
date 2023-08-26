namespace Biflow.DataAccess.Models;

public class PackageStepExecutionAttempt : StepExecutionAttempt
{

    public PackageStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Package)
    {
    }

    protected PackageStepExecutionAttempt(PackageStepExecutionAttempt other) : base(other)
    {
    }

    public PackageStepExecutionAttempt(PackageStepExecution execution) : base(execution) { }

    public long? PackageOperationId { get; set; }

    protected override StepExecutionAttempt Clone() => new PackageStepExecutionAttempt(this);
}
