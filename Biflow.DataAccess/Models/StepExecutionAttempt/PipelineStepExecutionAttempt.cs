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

    public PipelineStepExecutionAttempt(PipelineStepExecution execution) : base(execution) { }

    public string? PipelineRunId { get; set; }

    protected override StepExecutionAttempt Clone() => new PipelineStepExecutionAttempt(this);
}
