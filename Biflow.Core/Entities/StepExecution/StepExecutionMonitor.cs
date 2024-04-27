namespace Biflow.Core.Entities;

public class StepExecutionMonitor
{
    public required Guid ExecutionId { get; init; }

    public required Guid StepId { get; init; }

    public required Guid MonitoredExecutionId { get; init; }

    public required Guid MonitoredStepId { get; init; }

    public required MonitoringReason MonitoringReason { get; init; }

    public DateTimeOffset CreatedOn { get; private set; } = DateTimeOffset.Now;

    public StepExecution StepExecution { get; set; } = null!;

    public StepExecution MonitoredStepExecution { get; set; } = null!;
}
