using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public abstract partial class StepEditModalBase<TStep> : ComponentBase, IDisposable, IStepEditModal where TStep : Step
{
    [Inject] public MarkupHelperService MarkupHelper { get; set; } = null!;
    
    [Inject] public IHxMessengerService Messenger { get; set; } = null!;

    [Inject] public IDbContextFactory<EtlManagerContext> DbContextFactory { get; set; } = null!;

    [Parameter] public Job? Job { get; set; }

    [Parameter] public IEnumerable<Step>? Steps { get; set; }

    [Parameter] public Guid StepId { get; set; }

    [Parameter] public EventCallback<Step> OnStepSubmit { get; set; }

    internal abstract string FormId { get; }

    internal TStep Step { get; private set; } = null!;

    internal List<string> Tags { get; set; } = new();

    internal HxModal Modal { get; set; } = null!;

    internal string StepError { get; private set; } = string.Empty;

    internal bool ShowDependencies { get; set; } = false;

    private EtlManagerContext Context { get; set; } = null!;

    private IEnumerable<Tag>? AllTags { get; set; }

    private Guid PrevStepId { get; set; }

    internal async Task<InputTagsDataProviderResult> GetTagSuggestions(InputTagsDataProviderRequest request)
    {
        AllTags ??= await Context.Tags.ToListAsync();
        return new InputTagsDataProviderResult
        {
            Data = AllTags?
            .Select(t => t.TagName)
            .Where(t => t.ContainsIgnoreCase(request.UserInput))
            .Where(t => !Tags.Any(tag => t == tag))
            .OrderBy(t => t) ?? Enumerable.Empty<string>()
        };
    }

    protected virtual void ResetAddedEntities(EntityEntry entityEntry) { }

    protected virtual void ResetDeletedEntities(EntityEntry entityEntry) { }

    protected virtual (bool Result, string? ErrorMessage) StepValidityCheck(Step step) => (true, null);

    protected abstract Task<TStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId);

    protected abstract TStep CreateNewStep(Job job);

    protected override async Task OnParametersSetAsync()
    {
        if (StepId != Guid.Empty && StepId != PrevStepId)
        {
            await ResetContext();
            Step = await GetExistingStepAsync(Context, StepId);
            ResetTags();
            PrevStepId = StepId;
        }
        else if (StepId == Guid.Empty && Job is not null)
        {
            await ResetContext();
            Step = CreateNewStep(Job);
            ResetTags();
        }
    }

    private async Task ResetContext()
    {
        if (Context is not null)
            await Context.DisposeAsync();

        Context = await DbContextFactory.CreateDbContextAsync();
    }

    private void ResetTags() => Tags = Step?.Tags
        .Select(t => t.TagName)
        .OrderBy(t => t)
        .ToList() ?? new();

    public void ResetStepError() => StepError = string.Empty;

    internal void OnClosed()
    {
        // If the modal is being simply closed, reset any changes made to entities loaded from the database.
        // If the user saves their changes, SubmitStep() is called first and changes are saved.

        // Reset added entities.
        foreach (var entity in Context.ChangeTracker.Entries().Where(e => e.Entity is not null && e.State == EntityState.Added).ToList())
        {
            if (entity.Entity is Dependency dependency)
            {
                if (Step?.Dependencies.Contains(dependency) == true)
                    Step.Dependencies.Remove(dependency);
            }

            ResetAddedEntities(entity);

            entity.State = EntityState.Detached;
        }

        // Reset deleted entities.
        foreach (var entity in Context.ChangeTracker.Entries().Where(e => e.Entity is not null && e.State == EntityState.Deleted).ToList())
        {
            if (entity.Entity is Dependency dependency)
            {
                if (Step?.Dependencies.Contains(dependency) == false)
                    Step.Dependencies.Add(dependency);
            }

            ResetDeletedEntities(entity);

            entity.State = EntityState.Unchanged;
        }

        // Reset changed entities.
        Context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is not null)
            .ToList()
            .ForEach(e => e.State = EntityState.Unchanged);

        ResetTags();
        AllTags = null;
    }

    internal async Task SubmitStep()
    {
        StepError = string.Empty;

        (var result, var message) = StepValidityCheck(Step);
        if (!result)
        {
            StepError = message ?? string.Empty;
            return;
        }

        // Synchronize tags
        foreach (var text in Tags.Where(str => !Step.Tags.Any(t => t.TagName == str)))
        {
            // New tags
            var tag = AllTags?.FirstOrDefault(t => t.TagName == text) ?? new Tag(text);
            Step.Tags.Add(tag);
        }
        foreach (var tag in Step.Tags.Where(t => !Tags.Contains(t.TagName)).ToList() ?? Enumerable.Empty<Tag>())
        {
            Step.Tags.Remove(tag);
        }

        // Save changes.
        try
        {
            // Existing step
            if (Step.StepId != Guid.Empty)
            {
                Context.Attach(Step).State = EntityState.Modified;
            }
            // New step
            else
            {
                Context.Steps.Add(Step);
            }
            await Context.SaveChangesAsync();

            await OnStepSubmit.InvokeAsync(Step);
            await Modal.HideAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            Messenger.AddError("Concurrency error",
                "The step has been modified outside of this session. Reload the page to view the most recent settings.");
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error adding/editing step", $"{ex.Message}\n{ex.InnerException?.Message}");
        }
    }

    public async Task ShowAsync(bool showDependencies = false)
    {
        ShowDependencies = showDependencies;
        await Modal.ShowAsync();
    }

    public void Dispose() => Context?.Dispose();
}
