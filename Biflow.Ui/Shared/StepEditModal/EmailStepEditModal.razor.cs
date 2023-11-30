using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class EmailStepEditModal : StepEditModal<EmailStep>
{
    internal override string FormId => "email_step_edit_form";

    protected override Task<EmailStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.EmailSteps
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

    protected override EmailStep CreateNewStep(Job job) =>
        new(job.JobId)
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            StepParameters = new List<EmailStepParameter>(),
            Sources = new List<StepSource>(),
            Targets = new List<StepTarget>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    private static EmailStepParameter GenerateNewParameter() =>
        new() { ParameterValueType = ParameterValueType.String, ParameterValue = string.Empty };
}
