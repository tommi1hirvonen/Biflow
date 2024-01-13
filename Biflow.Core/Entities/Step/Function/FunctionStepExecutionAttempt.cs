using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class FunctionStepExecutionAttempt : StepExecutionAttempt
{

    public FunctionStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Function)
    {
    }

    public FunctionStepExecutionAttempt(FunctionStepExecutionAttempt other, int retryAttemptIndex) : base(other, retryAttemptIndex)
    {
    }

    public FunctionStepExecutionAttempt(FunctionStepExecution execution) : base(execution) { }

    [Display(Name = "Function instance id")]
    [MaxLength(250)]
    public string? FunctionInstanceId { get; set; }
}
