using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class ExecutionConcurrency
{
    public ExecutionConcurrency() { }

    public ExecutionConcurrency(JobConcurrency jobConcurrency, Execution execution)
    {
        ExecutionId = execution.ExecutionId;
        StepType = jobConcurrency.StepType;
        MaxParallelSteps = jobConcurrency.MaxParallelSteps;
    }

    public Guid ExecutionId { get; private set; }

    public StepType StepType { get; private set; }

    [Required]
    [Range(0, 100)]
    public int MaxParallelSteps { get; private set; }

    [JsonIgnore]
    public Execution Execution { get; private set; } = null!;
}
