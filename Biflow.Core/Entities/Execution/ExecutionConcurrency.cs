using System.ComponentModel.DataAnnotations;

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
    [Display(Name = "Max parallel steps (0 = use default)")]
    [Range(0, 100)]
    public int MaxParallelSteps { get; private set; }

    public Execution Execution { get; set; } = null!;
}
