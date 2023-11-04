namespace Biflow.DataAccess.Models;

public class PipelineStepExecutionAttempt : StepExecutionAttempt
{

    public PipelineStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Pipeline)
    {
    }

    public PipelineStepExecutionAttempt(PipelineStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public PipelineStepExecutionAttempt(PipelineStepExecution execution) : base(execution) { }

    public string? PipelineRunId { get; set; }
}
