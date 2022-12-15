using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionConcurrency")]
[PrimaryKey("ExecutionId", "StepType")]
public class ExecutionConcurrency
{
    public Guid ExecutionId { get; set; }

    public StepType StepType { get; set; }

    [Required]
    [Display(Name = "Max parallel steps (0 = use default)")]
    [Range(0, 100)]
    public int MaxParallelSteps { get; set; }

    public Execution Execution { get; set; } = null!;
}
