using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

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
