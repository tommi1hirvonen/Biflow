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

    public FunctionStepExecutionAttempt(FunctionStepExecution execution) : base(execution) { }

    [Display(Name = "Function instance id")]
    public string? FunctionInstanceId { get; set; }

    protected override StepExecutionAttempt Clone() => new FunctionStepExecutionAttempt(this);
}
