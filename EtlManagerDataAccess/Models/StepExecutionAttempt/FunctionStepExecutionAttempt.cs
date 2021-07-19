using System.ComponentModel.DataAnnotations;

namespace EtlManagerDataAccess.Models
{
    public record FunctionStepExecutionAttempt : StepExecutionAttempt
    {
        [Display(Name = "Function instance id")]
        public string? FunctionInstanceId { get; set; }

        public override void Reset()
        {
            base.Reset();
            FunctionInstanceId = null;
        }

    }
}
