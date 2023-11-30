using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class ExeStepEditModal : StepEditModal<ExeStep>
{
    internal override string FormId => "exe_step_edit_form";

    protected override ExeStep CreateNewStep(Job job) =>
        new(job.JobId)
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<StepSource>(),
            Targets = new List<StepTarget>(),
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
        .Include(step => step.Sources)
        .ThenInclude(s => s.DataObject)
        .Include(step => step.Targets)
        .ThenInclude(t => t.DataObject)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);
}
