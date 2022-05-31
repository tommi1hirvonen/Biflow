using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class ExecutionConditionParameter : ParameterBase
{
    [Display(Name = "Step id")]
    public Guid StepId { get; set; }

    public Step Step { get; set; } = null!;

    public Guid? JobParameterId { get; set; }

    public JobParameter? JobParameter { get; set; }
}
