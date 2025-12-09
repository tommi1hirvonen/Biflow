using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

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
[JsonDerivedType(typeof(DatabricksStep), nameof(StepType.Databricks))]
[JsonDerivedType(typeof(DbtStep), nameof(StepType.Dbt))]
[JsonDerivedType(typeof(ScdStep), nameof(StepType.Scd))]
[JsonDerivedType(typeof(DataflowStep), nameof(StepType.Dataflow))]
[JsonDerivedType(typeof(FabricStep), nameof(StepType.Fabric))]
[JsonDerivedType(typeof(HttpStep), nameof(StepType.Http))]
public abstract class Step : IComparable, IAuditable
{
    protected Step(StepType stepType)
    {
        StepType = stepType;
    }

    /// <summary>
    /// Used to initialize properties based on another <see cref="Step"/> and optionally on another <see cref="Job"/>
    /// </summary>
    /// <param name="other"><see cref="Step"/> used as a base to initialize the generated object's properties</param>
    /// <param name="job">Optionally provide a <see cref="Job"/> to switch the generated <see cref="Step"/>'s target job</param>
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
        ExecutionConditionParameters = other.ExecutionConditionParameters
            .Select(p => new ExecutionConditionParameter(p, this, job))
            .ToList();
        // Skip copying dependencies.
        // This is use case specific and potentially requires mapping between ids
        // and thus cannot be easily done in the constructor.
    }

    [Required]
    public Guid StepId { get; init; }

    [Required]
    [NotEmptyGuid]
    public Guid JobId { get; init; }

    [JsonIgnore]
    public Job Job { get; init; } = null!;

    [Required]
    [MaxLength(250)]
    public string? StepName { get; set; }

    public string? StepDescription
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? null : value;
    }

    [Required]
    public int ExecutionPhase { get; set; }

    public StepType StepType { get; }
    
    [JsonIgnore]
    public abstract DisplayStepType DisplayStepType { get; }

    public DuplicateExecutionBehaviour DuplicateExecutionBehaviour { get; set; } = DuplicateExecutionBehaviour.Wait;

    [Required]
    public bool IsEnabled { get; set; } = true;

    [Required]
    [Range(0, 10)]
    public int RetryAttempts { get; set; }

    [Required]
    [Range(0, 1000)]
    public double RetryIntervalMinutes { get; set; }

    public EvaluationExpression ExecutionConditionExpression { get; init; } = new();

    public DateTimeOffset CreatedOn { get; set; }

    [MaxLength(250)]
    public string? CreatedBy { get; set; }

    public DateTimeOffset LastModifiedOn { get; set; }

    [MaxLength(250)]
    public string? LastModifiedBy { get; set; }

    [JsonIgnore]
    public byte[]? Timestamp { get; [UsedImplicitly] private set; }

    [JsonInclude]
    public ICollection<Dependency> Dependencies { get; private set; } = new List<Dependency>();

    [JsonIgnore]
    public IEnumerable<Dependency> Depending { get; } = new List<Dependency>();

    [ValidateComplexType]
    [JsonInclude]
    public ICollection<StepDataObject> DataObjects { get; private set; } = new List<StepDataObject>();

    [ValidateComplexType]
    [JsonInclude]
    public ICollection<ExecutionConditionParameter> ExecutionConditionParameters { get; private set; } = new List<ExecutionConditionParameter>();

    [JsonInclude]
    public ICollection<StepTag> Tags { get; private set; } = new List<StepTag>();

    [JsonIgnore]
    public IEnumerable<StepSubscription> StepSubscriptions { get; } = new List<StepSubscription>();

    public int CompareTo(object? obj)
    {
        switch (obj)
        {
            case null:
                return 1;
            case Step other:
                var result = ExecutionPhase.CompareTo(other.ExecutionPhase);
                return result == 0
                    ? StepName?.CompareTo(other.StepName) ?? -1
                    : result;
            default:
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

    public abstract Step Copy(Job? targetJob = null);

    public abstract StepExecution ToStepExecution(Execution execution);
}
