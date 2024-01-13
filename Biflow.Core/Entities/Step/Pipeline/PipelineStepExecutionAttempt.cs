using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

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

    [MaxLength(250)]
    public string? PipelineRunId { get; set; }
}
