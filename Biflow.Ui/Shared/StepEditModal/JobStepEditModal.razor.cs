using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class JobStepEditModal : ParameterizedStepEditModal<JobStep>
{
    [Parameter] public IEnumerable<Job> Jobs { get; set; } = Enumerable.Empty<Job>();

    private JobStepParameterEditComponent? ParameterEdit { get; set; }

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
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            StepParameters = new List<StepParameterBase>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override async Task<JobStep> GetExistingStepAsync(BiflowContext context, Guid stepId)
    {
        var step = await context.JobSteps
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        SetJobToExecute(step.JobToExecuteId);
        return step;
    }

    private void SetJobToExecute(Guid? jobId)
    {
        if (Step is not null)
        {
            Step.JobToExecuteId = jobId;
            Step.StepParameters.Clear();
        }
    }

    private async Task<AutosuggestDataProviderResult<Job>> GetJobSuggestions(AutosuggestDataProviderRequest request)
    {
        await Task.Delay(150);
        return new AutosuggestDataProviderResult<Job>
        {
            Data = Jobs.Where(j => j.JobId != Job?.JobId)
                .Where(j => j.JobName.ContainsIgnoreCase(request.UserInput)
                        || (j.Category?.CategoryName.ContainsIgnoreCase(request.UserInput) ?? false))
        };
    }
}
