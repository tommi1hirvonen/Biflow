using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public abstract class Step : IComparable
{
    public Step(StepType stepType)
    {
        StepType = stepType;
    }

    [Key]
    [Required]
    public Guid StepId { get; set; }

    [Required]
    public Guid JobId { get; set; }

    public Job Job { get; set; } = null!;

    [Required]
    [MaxLength(250)]
    [Display(Name = "Step name")]
    public string? StepName { get; set; }

    [Display(Name = "Description")]
    public string? StepDescription
    {
        get => _stepDescription;
        set => _stepDescription = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _stepDescription;

    [Required]
    [Display(Name = "Execution phase")]
    public int ExecutionPhase { get; set; }

    [Required]
    [Display(Name = "Step type")]
    public StepType StepType { get; private init; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Last modified")]
    public DateTimeOffset LastModifiedDateTime { get; set; }

    [Required]
    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    [Required]
    [Display(Name = "Retry attempts")]
    [Range(0, 10)]
    public int RetryAttempts { get; set; }

    [Required]
    [Display(Name = "Retry interval (min)")]
    [Range(0, 1000)]
    public int RetryIntervalMinutes { get; set; }

    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public int TimeoutMinutes { get; set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Last modified by")]
    public string? LastModifiedBy { get; set; }

    [Timestamp]
    public byte[]? Timestamp { get; set; }

    public IList<Dependency> Dependencies { get; set; } = null!;

    public IList<DatabaseObject> Sources { get; set; } = null!;

    public IList<DatabaseObject> Targets { get; set; } = null!;

    public IList<Tag> Tags { get; set; } = null!;

    public IList<StepExecution> StepExecutions { get; set; } = null!;

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;

        if (obj is Step other)
        {
            int result = ExecutionPhase.CompareTo(other.ExecutionPhase);
            if (result == 0)
            {
                return StepName?.CompareTo(other.StepName) ?? 0;
            }
            else
            {
                return result;
            }
        }
        else
        {
            throw new ArgumentException("Object is not a Step");
        }
    }
}
