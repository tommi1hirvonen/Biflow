using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Attributes.Validation;

namespace Biflow.Core.Entities;

public class JobStep : Step, IHasStepParameters<JobStepParameter>, IHasTimeout
{
    [JsonConstructor]
    public JobStep() : base(StepType.Job) { }

    private JobStep(JobStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        JobToExecuteId = other.JobToExecuteId;
        JobToExecute = other.JobToExecute;
        JobExecuteSynchronized = other.JobExecuteSynchronized;
        TagFilters = other.TagFilters;
        StepParameters = other.StepParameters
            .Select(p => new JobStepParameter(p, this, targetJob))
            .ToList();
    }

    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }
    
    [NotEmptyGuid]
    public Guid JobToExecuteId { get; set; }

    /// <summary>
    /// In order for cross-job dependencies to work reliably across parent-child job executions,
    /// synchronized execution should be enabled. This ensures the parent executions are kept in orchestration scope
    /// until all child executions have also completed.
    /// </summary>
    [Required]
    public bool JobExecuteSynchronized { get; set; }

    [JsonIgnore]
    public Job JobToExecute { get; set; } = null!;

    [ValidateComplexType]
    [JsonInclude]
    public IList<JobStepParameter> StepParameters { get; private set; } = new List<JobStepParameter>();

    [JsonInclude]
    public ICollection<StepTag> TagFilters { get; private set; } = new List<StepTag>();

    public override JobStep Copy(Job? targetJob = null) => new(this, targetJob);

    public override StepExecution ToStepExecution(Execution execution) => new JobStepExecution(this, execution);
}
