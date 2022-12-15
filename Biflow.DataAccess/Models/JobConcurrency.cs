using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("JobConcurrency")]
[PrimaryKey("JobId", "StepType")]
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
