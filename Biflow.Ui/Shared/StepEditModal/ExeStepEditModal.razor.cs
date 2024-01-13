using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class ExeStepEditModal : StepEditModal<ExeStep>
{
    internal override string FormId => "exe_step_edit_form";

    protected override ExeStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            DataObjects = new List<StepDataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>(),
            StepParameters = new List<ExeStepParameter>()
        };

    protected override Task<ExeStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.ExeSteps
        .Include(step => step.Job)
        .ThenInclude(job => job.JobParameters)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.InheritFromJobParameter)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.ExpressionParameters)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.DataObjects)
        .ThenInclude(s => s.DataObject)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);
}
