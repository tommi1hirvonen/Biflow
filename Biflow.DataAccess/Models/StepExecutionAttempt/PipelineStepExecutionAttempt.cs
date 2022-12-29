namespace Biflow.DataAccess.Models;

public class PipelineStepExecutionAttempt : StepExecutionAttempt
{

    public PipelineStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Pipeline)
    {
    }

    protected PipelineStepExecutionAttempt(PipelineStepExecutionAttempt other) : base(other)
    {
    }

    public string? PipelineRunId { get; set; }

    public override StepExecutionAttempt Clone() => new PipelineStepExecutionAttempt(this);
}
