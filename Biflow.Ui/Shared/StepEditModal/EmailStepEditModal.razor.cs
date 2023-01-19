using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class EmailStepEditModal : StepEditModal<EmailStep>
{
    internal override string FormId => "email_step_edit_form";

    protected override Task<EmailStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.EmailSteps
        .Include(step => step.Job)
        .ThenInclude(job => job.JobParameters)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.InheritFromJobParameter)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    protected override EmailStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            StepParameters = new List<EmailStepParameter>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    private static EmailStepParameter GenerateNewParameter() =>
        new() { ParameterValueType = ParameterValueType.String, ParameterValue = string.Empty };
}
