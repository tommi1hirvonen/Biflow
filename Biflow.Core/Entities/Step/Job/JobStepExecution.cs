using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class JobStepExecution : StepExecution, IHasStepExecutionParameters<JobStepExecutionParameter>
{
    public JobStepExecution(string stepName) : base(stepName, StepType.Job)
    {
    }

    public JobStepExecution(JobStep step, Execution execution) : base(step, execution)
    {
        ArgumentNullException.ThrowIfNull(step.JobToExecuteId);

        JobToExecuteId = (Guid)step.JobToExecuteId;
        JobExecuteSynchronized = step.JobExecuteSynchronized;
        TagFilters = step.TagFilters
            .Select(t => new TagFilter(t.TagId, t.TagName))
            .ToList();
        StepExecutionParameters = step.StepParameters
            .Select(p => new JobStepExecutionParameter(p, this))
            .ToArray();
        StepExecutionAttempts.Add(new JobStepExecutionAttempt(this));
    }

    [Display(Name = "Job to execute")]
    public Guid JobToExecuteId { get; private set; }

    [Display(Name = "Synchronized")]
    public bool JobExecuteSynchronized { get; private set; }

    public IEnumerable<JobStepExecutionParameter> StepExecutionParameters { get; } = new List<JobStepExecutionParameter>();

    /// <summary>
    /// List of tag tuples that should be used to filter steps in executed job.
    /// </summary>
    public List<TagFilter> TagFilters { get; set; } = [];

    public record TagFilter(Guid TagId, string TagName);
}
