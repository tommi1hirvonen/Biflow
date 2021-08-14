using System.ComponentModel.DataAnnotations;

namespace EtlManagerDataAccess.Models
{
    public record FunctionStepExecutionAttempt : StepExecutionAttempt
    {

        public FunctionStepExecutionAttempt(StepExecutionStatus executionStatus)
            : base(executionStatus, StepType.Function)
        {
        }

        [Display(Name = "Function instance id")]
        public string? FunctionInstanceId { get; set; }

        public override void Reset()
        {
            base.Reset();
            FunctionInstanceId = null;
        }

    }
}
