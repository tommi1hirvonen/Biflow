using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class StepExecutionMonitor
{
    public required Guid ExecutionId { get; init; }

    public required Guid StepId { get; init; }

    public required Guid MonitoredExecutionId { get; init; }

    public required Guid MonitoredStepId { get; init; }

    public required MonitoringReason MonitoringReason { get; init; }

    public DateTimeOffset CreatedOn { get; private set; } = DateTimeOffset.Now;

    [JsonIgnore]
    public StepExecution StepExecution { get; init; } = null!;

    [JsonIgnore]
    public StepExecution MonitoredStepExecution { get; init; } = null!;
}
