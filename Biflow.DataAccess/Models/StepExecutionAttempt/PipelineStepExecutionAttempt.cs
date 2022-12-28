namespace Biflow.DataAccess.Models;

public record PipelineStepExecutionAttempt : StepExecutionAttempt
{

    public PipelineStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Pipeline)
    {
    }

    [IncludeInReset]
    public string? PipelineRunId { get; set; }
}
