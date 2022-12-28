using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public record FunctionStepExecutionAttempt : StepExecutionAttempt
{

    public FunctionStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Function)
    {
    }

    [IncludeInReset]
    [Display(Name = "Function instance id")]
    public string? FunctionInstanceId { get; set; }
}
