using Biflow.DataAccess;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class JobStepEditModal : StepEditModal<JobStep>
{
    internal override string FormId => "job_step_edit_form";

    private List<string> tagFilters = [];

    private IEnumerable<JobCategory?> JobCategories => JobSlims?.Values
        .Select(j => j.Category)
        .Distinct()
        .OrderBy(c => c is null)
        .ThenBy(c => c?.CategoryName)
        ?? Enumerable.Empty<JobCategory?>();

    private async Task<InputTagsDataProviderResult> GetTagFilterSuggestions(InputTagsDataProviderRequest request)
    {
        await Task.Delay(50); // needed for the HxInputTags component to behave correctly (reopen dropdown after selecting one tag)
        await EnsureAllTagsInitialized();
        return new InputTagsDataProviderResult
        {
            Data = AllTags?
            .Select(t => t.TagName)
            .Where(t => t.ContainsIgnoreCase(request.UserInput))
            .Where(t => !tagFilters.Any(tag => t == tag))
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
            JobToExecuteId = null,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            DataObjects = new List<StepDataObject>(),
            StepParameters = new List<JobStepParameter>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>(),
            TagFilters = new List<Tag>()
        };

    protected override async Task<JobStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        var step = await context.JobSteps
            .Include(step => step.Job)
            .ThenInclude(job => job.JobParameters)
            .Include(step => step.Tags)
            .Include(step => step.TagFilters)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        SetJobToExecute();
        tagFilters = step.TagFilters
                .Select(t => t.TagName)
                .OrderBy(t => t)
                .ToList();
        return step;
    }

    protected override Task OnSubmitAsync(JobStep step)
    {
        // Synchronize tags
        foreach (var text in tagFilters.Where(str => !step.TagFilters.Any(t => t.TagName == str)))
        {
            // New tags
            var tag = AllTags?.FirstOrDefault(t => t.TagName == text) ?? new Tag(text);
            step.TagFilters.Add(tag);
        }
        foreach (var tag in step.TagFilters.Where(t => !tagFilters.Contains(t.TagName)).ToList() ?? [])
        {
            step.TagFilters.Remove(tag);
        }
        return Task.CompletedTask;
    }

    private void SetJobToExecute()
    {
        Step?.StepParameters.Clear();
    }

}
