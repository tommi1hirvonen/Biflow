using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class JobConcurrency
{
    [Key]
    public Guid JobId { get; set; }

    [Key]
    public StepType StepType { get; set; }

    [Required]
    [Display(Name = "Max parallel steps (0 = use default)")]
    [Range(0, 100)]
    public int MaxParallelSteps { get; set; }

    public Job Job { get; set; } = null!;
}
