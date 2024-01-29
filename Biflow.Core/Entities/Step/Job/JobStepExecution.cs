using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class JobStepExecution : StepExecution,
    IHasStepExecutionParameters<JobStepExecutionParameter>,
    IHasStepExecutionAttempts<JobStepExecutionAttempt>
{
    public JobStepExecution(string stepName) : base(stepName, StepType.Job)
    {
    }

    public JobStepExecution(JobStep step, Execution execution) : base(step, execution)
    {
        ArgumentNullException.ThrowIfNull(step.JobToExecuteId);

        JobToExecuteId = (Guid)step.JobToExecuteId;
        JobExecuteSynchronized = step.JobExecuteSynchronized;
        _tagFilters = step.TagFilters
            .Select(t => new TagFilter(t.TagId, t.TagName))
            .ToList();
        StepExecutionParameters = step.StepParameters
            .Select(p => new JobStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new JobStepExecutionAttempt(this));
    }

    private readonly List<TagFilter> _tagFilters = [];

    [Display(Name = "Job to execute")]
    public Guid JobToExecuteId { get; private set; }

    [Display(Name = "Synchronized")]
    public bool JobExecuteSynchronized { get; private set; }

    public IEnumerable<JobStepExecutionParameter> StepExecutionParameters { get; } = new List<JobStepExecutionParameter>();

    /// <summary>
    /// List of tag tuples that should be used to filter steps in executed job.
    /// </summary>
    public IEnumerable<TagFilter> TagFilters => _tagFilters;

    public override JobStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new JobStepExecutionAttempt((JobStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    public record TagFilter(Guid TagId, string TagName);
}
