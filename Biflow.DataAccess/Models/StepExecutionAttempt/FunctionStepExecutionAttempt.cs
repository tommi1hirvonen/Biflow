using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public record FunctionStepExecutionAttempt : StepExecutionAttempt
{

    public FunctionStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Function)
    {
    }

    [Display(Name = "Function instance id")]
    public string? FunctionInstanceId { get; set; }

    protected override void ResetInstanceMembers()
    {
        FunctionInstanceId = null;
    }
}
