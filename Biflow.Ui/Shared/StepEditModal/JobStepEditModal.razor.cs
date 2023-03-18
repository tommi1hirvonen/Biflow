using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class JobStepEditModal : StepEditModal<JobStep>
{
    [Parameter] public IEnumerable<Job> Jobs { get; set; } = Enumerable.Empty<Job>();

    internal override string FormId => "job_step_edit_form";

    private List<string> TagFilters { get; set; } = new();

    private IEnumerable<JobCategory?> JobCategories => Jobs
        .Select(j => j.Category)
        .Distinct()
        .OrderBy(c => c is null)
        .ThenBy(c => c?.CategoryName);

    private async Task<InputTagsDataProviderResult> GetTagFilterSuggestions(InputTagsDataProviderRequest request)
    {
        await Task.Delay(50); // needed for the HxInputTags component to behave correctly (reopen dropdown after selecting one tag)
        await EnsureAllTagsInitialized();
        return new InputTagsDataProviderResult
        {
            Data = AllTags?
            .Select(t => t.TagName)
            .Where(t => t.ContainsIgnoreCase(request.UserInput))
            .Where(t => !TagFilters.Any(tag => t == tag))
            .OrderBy(t => t) ?? Enumerable.Empty<string>()
        };
    }

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
            .Include(step => step.TagFilters)
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
        TagFilters = step.TagFilters
                .Select(t => t.TagName)
                .OrderBy(t => t)
                .ToList();
        return step;
    }

    protected override void OnSubmit(JobStep step)
    {
        // Synchronize tags
        foreach (var text in TagFilters.Where(str => !step.TagFilters.Any(t => t.TagName == str)))
        {
            // New tags
            var tag = AllTags?.FirstOrDefault(t => t.TagName == text) ?? new Tag(text);
            step.TagFilters.Add(tag);
        }
        foreach (var tag in step.TagFilters.Where(t => !TagFilters.Contains(t.TagName)).ToList() ?? Enumerable.Empty<Tag>())
        {
            step.TagFilters.Remove(tag);
        }
    }

    private void SetJobToExecute()
    {
        Step?.StepParameters.Clear();
    }

}
