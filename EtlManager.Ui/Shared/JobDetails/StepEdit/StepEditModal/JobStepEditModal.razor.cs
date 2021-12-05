using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public partial class JobStepEditModal : StepEditModalBase<JobStep>
{
    [Parameter] public IEnumerable<Job> Jobs { get; set; } = Enumerable.Empty<Job>();

    internal override string FormId => "job_step_edit_form";

    protected override JobStep CreateNewStep(Job job)
    {
        return new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            JobToExecuteId = Jobs.FirstOrDefault(job => job.JobId != Job?.JobId)?.JobId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>()
        };
    }

    protected override async Task<JobStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId)
    {
        return await context.JobSteps
                .Include(step => step.Tags)
                .Include(step => step.Dependencies)
                .FirstAsync(step => step.StepId == stepId);
    }
}
