using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("StepConditionParameter")]
[PrimaryKey("ParameterId")]
public class ExecutionConditionParameter : ParameterBase
{
    [Display(Name = "Step id")]
    public Guid StepId { get; set; }

    public Step Step { get; set; } = null!;

    public Guid? JobParameterId { get; set; }

    public JobParameter? JobParameter { get; set; }
}
