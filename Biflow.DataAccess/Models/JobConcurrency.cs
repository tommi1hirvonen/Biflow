using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class JobConcurrency
{
    public Guid JobId { get; set; }

    public StepType StepType { get; set; }

    [Required]
    [Display(Name = "Max parallel steps (0 = use default)")]
    [Range(0, 100)]
    public int MaxParallelSteps { get; set; }

    public Job Job { get; set; } = null!;
}
