using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("JobConcurrency")]
[PrimaryKey("JobId", "StepType")]
public class JobConcurrency
{
    public JobConcurrency() { }

    internal JobConcurrency(JobConcurrency other, Job job)
    {
        JobId = job.JobId;
        Job = job;
        MaxParallelSteps = other.MaxParallelSteps;
        StepType = other.StepType;
    }

    public Guid JobId { get; set; }

    public StepType StepType { get; set; }

    [Required]
    [Display(Name = "Max parallel steps (0 = use default)")]
    [Range(0, 100)]
    public int MaxParallelSteps { get; set; }

    [JsonIgnore]
    public Job Job { get; set; } = null!;
}
