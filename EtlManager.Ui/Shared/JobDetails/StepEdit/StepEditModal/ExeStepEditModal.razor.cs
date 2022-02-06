using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public partial class ExeStepEditModal : StepEditModalBase<ExeStep>
{
    internal override string FormId => "exe_step_edit_form";

    protected override ExeStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<SourceTargetObject>(),
            Targets = new List<SourceTargetObject>()
        };

    protected override Task<ExeStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId) =>
        context.ExeSteps
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .FirstAsync(step => step.StepId == stepId);
}
