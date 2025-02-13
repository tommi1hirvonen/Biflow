using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

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

    public Guid JobId { get; init; }

    public StepType StepType { get; init; }

    [Required]
    [Range(0, 100)]
    public int MaxParallelSteps { get; set; }

    [JsonIgnore]
    public Job Job { get; init; } = null!;
}
