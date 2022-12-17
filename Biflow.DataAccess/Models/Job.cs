using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("Job")]
public class Job
{
    [Key]
    public Guid JobId { get; set; }

    [Required]
    [MaxLength(250)]
    [Display(Name = "Job name")]
    public string JobName { get; set; } = "";

    [Display(Name = "Description")]
    public string? JobDescription
    {
        get => _jobDescription;
        set => _jobDescription = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _jobDescription;

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Created")]
    public DateTimeOffset CreatedDateTime { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Last modified")]
    public DateTimeOffset LastModifiedDateTime { get; set; }

    [Required]
    [Display(Name = "Use dependency mode")]
    public bool UseDependencyMode { get; set; }

    [Required]
    [Display(Name = "Stop on first error")]
    public bool StopOnFirstError { get; set; }

    [Required]
    [Display(Name = "Max parallel steps (0 = use default)")]
    [Range(0, 100)]
    public int MaxParallelSteps { get; set; }

    [Required]
    [Display(Name = "Overtime notification limit (min, 0 = indefinite)")]
    [Range(0, 10000)]
    public int OvertimeNotificationLimitMinutes { get; set; }

    [Required]
    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; }

    [Column("JobCategoryId")]
    public Guid? CategoryId { get; set; }

    public JobCategory? Category { get; set; }

    public ICollection<Step> Steps { get; set; } = null!;
    
    public ICollection<JobStep> JobSteps { get; set; } = null!;
    
    public ICollection<Schedule> Schedules { get; set; } = null!;
    
    public ICollection<Execution> Executions { get; set; } = null!;
    
    public ICollection<Subscription> Subscriptions { get; set; } = null!;
    
    public ICollection<JobParameter> JobParameters { get; set; } = null!;

    [ValidateComplexType]
    public ICollection<JobConcurrency> JobConcurrencies { get; set; } = null!;

    public ICollection<User> Users { get; set; } = null!;

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Last modified by")]
    public string? LastModifiedBy { get; set; }

    [Timestamp]
    public byte[]? Timestamp { get; set; }
}
