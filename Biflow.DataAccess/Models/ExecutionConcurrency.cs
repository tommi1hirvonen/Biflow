using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("ExecutionConcurrency")]
[PrimaryKey("ExecutionId", "StepType")]
public class ExecutionConcurrency
{
    public Guid ExecutionId { get; private set; }

    public StepType StepType { get; private set; }

    [Required]
    [Display(Name = "Max parallel steps (0 = use default)")]
    [Range(0, 100)]
    public int MaxParallelSteps { get; private set; }

    public Execution Execution { get; set; } = null!;
}
