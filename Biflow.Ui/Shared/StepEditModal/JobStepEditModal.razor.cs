using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class JobStepEditModal : StepEditModal<JobStep>
{
    [Parameter] public IEnumerable<Job> Jobs { get; set; } = Enumerable.Empty<Job>();

    internal override string FormId => "job_step_edit_form";

    private IEnumerable<JobCategory?> JobCategories => Jobs
        .Select(j => j.Category)
        .Distinct()
        .OrderBy(c => c is null)
        .ThenBy(c => c?.CategoryName);

    protected override JobStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            JobToExecuteId = null,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            StepParameters = new List<JobStepParameter>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override async Task<JobStep> GetExistingStepAsync(BiflowContext context, Guid stepId)
    {
        var step = await context.JobSteps
            .Include(step => step.Job)
            .ThenInclude(job => job.JobParameters)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        SetJobToExecute();
        return step;
    }

    private void SetJobToExecute()
    {
        if (Step is not null)
        {
            Step.StepParameters.Clear();
        }
    }

}
