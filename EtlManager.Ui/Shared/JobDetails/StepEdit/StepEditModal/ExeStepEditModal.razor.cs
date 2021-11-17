using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public partial class ExeStepEditModal : StepEditModalBase<ExeStep>
{
    internal override string FormId => "exe_step_edit_form";

    protected override ExeStep CreateNewStep(Job job)
    {
        return new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>()
        };
    }

    protected override async Task<ExeStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId)
    {
        return await context.ExeSteps
                .Include(step => step.Tags)
                .Include(step => step.Dependencies)
                .FirstAsync(step => step.StepId == stepId);
    }
}
