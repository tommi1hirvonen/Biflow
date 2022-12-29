using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class FunctionStepExecutionAttempt : StepExecutionAttempt
{

    public FunctionStepExecutionAttempt(StepExecutionStatus executionStatus)
        : base(executionStatus, StepType.Function)
    {
    }

    protected FunctionStepExecutionAttempt(FunctionStepExecutionAttempt other) : base(other)
    {
    }

    [Display(Name = "Function instance id")]
    public string? FunctionInstanceId { get; set; }

    public override StepExecutionAttempt Clone() => new FunctionStepExecutionAttempt(this);
}
