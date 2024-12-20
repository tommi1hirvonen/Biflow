using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class Job : IAuditable
{
    public Job() { }

    private Job(Job other)
    {
        JobId = Guid.NewGuid();
        JobName = other.JobName;
        JobDescription = other.JobDescription;
        ExecutionMode = other.ExecutionMode;
        StopOnFirstError = other.StopOnFirstError;
        MaxParallelSteps = other.MaxParallelSteps;
        TimeoutMinutes = other.TimeoutMinutes;
        OvertimeNotificationLimitMinutes = other.OvertimeNotificationLimitMinutes;
        IsEnabled = other.IsEnabled;
        Tags = other.Tags.ToList();
        JobConcurrencies = other.JobConcurrencies
            .Select(c => new JobConcurrency(c, this))
            .ToList();
        JobParameters = other.JobParameters
            .Select(p => new JobParameter(p, this))
            .ToList();

        // While creating copies of steps,
        // also create a mapping dictionary to map dependencies based on old step ids.
        var mapping = other.Steps
            .Select(s => (Original: s, Copy: s.Copy(this)))
            .ToDictionary(x => x.Original.StepId, x => x);

        Steps = mapping.Values
            .Select(map =>
            {
                // Map dependencies from ids to new ids.
                map.Copy.Dependencies.AddRange(map.Original.Dependencies.Select(d => MapDependency(map.Copy, d)));
                return map.Copy;
            })
            .ToList();
        return;

        Dependency MapDependency(Step copy, Dependency dep)
        {
            // Map the dependent step's id from an old value to a new value using the dictionary.
            // In case no matching key is found, it is likely a cross-job dependency => use the id as is.
            var dependentOn = mapping.TryGetValue(dep.DependantOnStepId, out var map) ? map.Copy.StepId : dep.DependantOnStepId;
            return new Dependency
            {
                StepId = copy.StepId,
                DependantOnStepId = dependentOn,
                DependencyType = dep.DependencyType
            };
        }
    }

    [JsonInclude]
    public Guid JobId { get; private set; }

    [Required]
    [MaxLength(250)]
    [Display(Name = "Job name")]
    public string JobName { get; set; } = "";

    [Display(Name = "Description")]
    public string? JobDescription
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? null : value;
    }

    [Required]
    [Display(Name = "Use dependency mode")]
    public ExecutionMode ExecutionMode { get; set; }

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
    public double OvertimeNotificationLimitMinutes { get; set; }

    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;
    
    public bool IsPinned { get; set; }

    [ValidateComplexType]
    [JsonInclude]
    public IList<JobParameter> JobParameters { get; private set; } = new List<JobParameter>();

    [ValidateComplexType]
    [JsonInclude]
    public ICollection<JobConcurrency> JobConcurrencies { get; private set; } = new List<JobConcurrency>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonInclude]
    public ICollection<Step> Steps { get; private set; } = new List<Step>();

    [JsonIgnore]
    public IEnumerable<JobStep> JobSteps { get; } = new List<JobStep>();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonInclude]
    public ICollection<Schedule> Schedules { get; private set; } = new List<Schedule>();

    [JsonIgnore]
    public IEnumerable<JobSubscription> JobSubscriptions { get; } = new List<JobSubscription>();

    [JsonIgnore]
    public IEnumerable<JobTagSubscription> JobTagSubscriptions { get; } = new List<JobTagSubscription>();

    [JsonIgnore]
    public IEnumerable<User> Users { get; } = new List<User>();

    [JsonInclude]
    public ICollection<JobTag> Tags { get; private set; } = new List<JobTag>();

    [Display(Name = "Created")]
    public DateTimeOffset CreatedOn { get; set; }

    [Display(Name = "Created by")]
    [MaxLength(250)]
    public string? CreatedBy { get; set; }

    [Display(Name = "Last modified")]
    public DateTimeOffset LastModifiedOn { get; set; }

    [Display(Name = "Last modified by")]
    [MaxLength(250)]
    public string? LastModifiedBy { get; set; }

    [JsonIgnore]
    public byte[]? Timestamp { get; [UsedImplicitly] private set; }

    public Job Copy() => new(this);
}
