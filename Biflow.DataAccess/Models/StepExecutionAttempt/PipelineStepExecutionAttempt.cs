namespace Biflow.DataAccess.Models;

public record PipelineStepExecutionAttempt : StepExecutionAttempt
{

    public PipelineStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Pipeline)
    {
    }

    public string? PipelineRunId { get; set; }

    protected override void ResetInstanceMembers()
    {
        PipelineRunId = null;
    }
}
