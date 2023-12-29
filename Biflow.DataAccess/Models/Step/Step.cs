using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("Step")]
[JsonDerivedType(typeof(AgentJobStep), nameof(StepType.AgentJob))]
[JsonDerivedType(typeof(DatasetStep), nameof(StepType.Dataset))]
[JsonDerivedType(typeof(EmailStep), nameof(StepType.Email))]
[JsonDerivedType(typeof(ExeStep), nameof(StepType.Exe))]
[JsonDerivedType(typeof(FunctionStep), nameof(StepType.Function))]
[JsonDerivedType(typeof(JobStep), nameof(StepType.Job))]
[JsonDerivedType(typeof(PackageStep), nameof(StepType.Package))]
[JsonDerivedType(typeof(PipelineStep), nameof(StepType.Pipeline))]
[JsonDerivedType(typeof(QlikStep), nameof(StepType.Qlik))]
[JsonDerivedType(typeof(SqlStep), nameof(StepType.Sql))]
[JsonDerivedType(typeof(TabularStep), nameof(StepType.Tabular))]
public abstract class Step : IComparable, IAuditable, ISoftDeletable
{
    public Step(StepType stepType)
    {
        StepType = stepType;
    }

    /// <summary>
    /// Used to initialize properties based on another <see cref="Step"/> and optionally on another <see cref="Models.Job"/>
    /// </summary>
    /// <param name="other"><see cref="Step"/> used as a base to initialize the generated object's properties</param>
    /// <param name="job">Optionally provide a <see cref="Models.Job"/> to swith the generated <see cref="Step"/>'s target job</param>
    protected Step(Step other, Job? job)
    {
        StepId = Guid.NewGuid();
        JobId = job?.JobId ?? other.JobId;
        Job = job ?? other.Job;
        StepName = other.StepName;
        StepDescription = other.StepDescription;
        ExecutionPhase = other.ExecutionPhase;
        StepType = other.StepType;
        DuplicateExecutionBehaviour = other.DuplicateExecutionBehaviour;
        CreatedOn = DateTimeOffset.Now;
        LastModifiedOn = DateTimeOffset.Now;
        IsEnabled = other.IsEnabled;
        RetryAttempts = other.RetryAttempts;
        RetryIntervalMinutes = other.RetryIntervalMinutes;
        ExecutionConditionExpression = new() { Expression = other.ExecutionConditionExpression.Expression };
        DataObjects = other.DataObjects
            .Select(d => new StepDataObject
            {
                StepId = StepId,
                Step = this,
                ObjectId = d.DataObject.ObjectId,
                DataObject = d.DataObject,
                ReferenceType = d.ReferenceType,
                DataAttributes = d.DataAttributes.ToList()
            })
            .ToList();
        Tags = other.Tags.ToList();
        Dependencies = job is null // If step is being copied to the same job, duplicate dependencies.
            ? other.Dependencies.Select(d => new Dependency(d, this)).ToList()
            : [];
        ExecutionConditionParameters = other.ExecutionConditionParameters
            .Select(p => new ExecutionConditionParameter(p, this, job))
            .ToList();
    }

    [Key]
    [Required]
    [JsonInclude]
    public Guid StepId { get; private set; }

    [Required]
    [NotEmptyGuid]
    [JsonInclude]
    public Guid JobId { get; init; }

    [JsonIgnore]
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

    [Display(Name = "Step type")]
    public StepType StepType { get; }

    public DuplicateExecutionBehaviour DuplicateExecutionBehaviour { get; set; } = DuplicateExecutionBehaviour.Wait;

    [Required]
    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    [Required]
    [Display(Name = "Retry attempts")]
    [Range(0, 10)]
    public int RetryAttempts { get; set; }

    [Required]
    [Display(Name = "Retry interval (min)")]
    [Range(0, 1000)]
    public double RetryIntervalMinutes { get; set; }

    public EvaluationExpression ExecutionConditionExpression { get; set; } = new();

    public DateTimeOffset CreatedOn { get; set; }

    [Display(Name = "Created by")]
    [MaxLength(250)]
    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModifiedOn { get; set; }

    [Display(Name = "Last modified by")]
    [MaxLength(250)]
    public string? LastModifiedBy { get; set; }

    [Timestamp]
    public byte[]? Timestamp { get; private set; }

    public DateTimeOffset? DeletedOn { get; set; }

    public IList<Dependency> Dependencies { get; set; } = null!;

    public IList<Dependency> Depending { get; set; } = null!;

    [ValidateComplexType]
    public IList<StepDataObject> DataObjects { get; set; } = null!;

    [ValidateComplexType]
    public IList<ExecutionConditionParameter> ExecutionConditionParameters { get; set; } = null!;

    public IList<Tag> Tags { get; set; } = null!;

    [JsonIgnore]
    public ICollection<StepSubscription> StepSubscriptions { get; set; } = null!;

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

    public async Task<object?> EvaluateExecutionConditionAsync()
    {
        var parameters = new Dictionary<string, object?>();
        foreach (var parameter in ExecutionConditionParameters)
        {
            parameters[parameter.ParameterName] = await parameter.EvaluateAsync();
        }
        var result = await ExecutionConditionExpression.EvaluateAsync(parameters);
        return result;
    }

    internal abstract Step Copy(Job? targetJob = null);

    internal abstract StepExecution ToStepExecution(Execution execution);
}
