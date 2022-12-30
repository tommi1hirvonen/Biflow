using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class JobStepEditModal : StepEditModalBase<JobStep>
{
    [Parameter] public IEnumerable<Job> Jobs { get; set; } = Enumerable.Empty<Job>();

    internal override string FormId => "job_step_edit_form";

    protected override JobStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            JobToExecuteId = null,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<SourceTargetObject>(),
            Targets = new List<SourceTargetObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<JobStep> GetExistingStepAsync(BiflowContext context, Guid stepId) => 
        context.JobSteps
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private async Task<AutosuggestDataProviderResult<Job>> GetJobSuggestions(AutosuggestDataProviderRequest request)
    {
        await Task.Delay(150);
        return new AutosuggestDataProviderResult<Job>
        {
            Data = Jobs.Where(j => j.JobName.ContainsIgnoreCase(request.UserInput)
                            || (j.Category?.CategoryName.ContainsIgnoreCase(request.UserInput) ?? false))
        };
    }
}
