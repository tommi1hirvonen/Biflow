using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public abstract partial class StepEditModal<TStep> : ComponentBase, IDisposable, IStepEditModal where TStep : Step
{    
    [Inject] public IHxMessengerService Messenger { get; set; } = null!;

    [Inject] public IDbContextFactory<BiflowContext> DbContextFactory { get; set; } = null!;

    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    [Parameter] public Action? OnModalClosed { get; set; }

    [Parameter] public EventCallback<Step> OnStepSubmit { get; set; }
    
    [Parameter] public IEnumerable<SqlConnectionInfo> Connections { get; set; } = Enumerable.Empty<SqlConnectionInfo>();

    internal abstract string FormId { get; }

    internal TStep? Step { get; private set; }

    internal List<string> Tags { get; set; } = new();

    internal HxModal? Modal { get; set; }

    internal StepEditModalView CurrentView { get; set; } = StepEditModalView.Settings;

    private BiflowContext Context { get; set; } = null!;

    protected IEnumerable<Tag>? AllTags { get; private set; }

    private IEnumerable<DataObject>? DataObjects { get; set; }

    internal bool Saving { get; set; } = false;

    internal async Task<InputTagsDataProviderResult> GetTagSuggestions(InputTagsDataProviderRequest request)
    {
        await Task.Delay(50); // needed for the HxInputTags component to behave correctly (reopen dropdown after selecting one tag)
        await EnsureAllTagsInitialized();
        return new InputTagsDataProviderResult
        {
            Data = AllTags?
            .Select(t => t.TagName)
            .Where(t => t.ContainsIgnoreCase(request.UserInput))
            .Where(t => !Tags.Any(tag => t == tag))
            .OrderBy(t => t) ?? Enumerable.Empty<string>()
        };
    }

    protected async Task EnsureAllTagsInitialized() => AllTags ??= await Context.Tags.ToListAsync();

    public async Task<IEnumerable<DataObject>> GetDataObjectsAsync()
    {
        DataObjects ??= await Context.DataObjects.ToListAsync();
        return DataObjects;
    }

    /// <summary>
    /// Called during OnParametersSetAsync() to load an existing Step from BiflowContext.
    /// The Step loaded from the context should be tracked in order to track changes made to the object.
    /// </summary>
    /// <param name="context">Instance of BiflowContext</param>
    /// <param name="stepId">Id of an existing Step that is to be edited</param>
    /// <returns></returns>
    protected abstract Task<TStep> GetExistingStepAsync(BiflowContext context, Guid stepId);

    /// <summary>
    /// Called during OnParametersSetAsync() if a new Step is being created.
    /// The method should return an "empty" instance of Step with its navigation properties correctly initialized.
    /// </summary>
    /// <param name="job">The job in which the Step is created</param>
    /// <returns></returns>
    protected abstract TStep CreateNewStep(Job job);

    /// <summary>
    /// Called when the step is submitted.
    /// Invoking takes place after tags and other objects are mapped but before the step is saved.
    /// </summary>
    protected virtual void OnSubmit(TStep step) { }

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

    internal void OnClosed()
    {
        Step = null;
        AllTags = null;
        DataObjects = null;
    }

    internal async Task SubmitStep()
    {
        Saving = true;

        try
        {
            if (Step is null)
            {
                Messenger.AddError("Error submitting step", "Step was null");
                return;
            }

            await MapExistingObjectsAsync(Step.Sources);
            await MapExistingObjectsAsync(Step.Targets);

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

            OnSubmit(Step);

            // Save changes.

            // New step
            if (Step.StepId == Guid.Empty)
            {
                Context.Steps.Add(Step);
            }
            // If the Step was an existing Step, the context has been tracking its changes.
            // => No need to attach it to the context separately.
            await Context.SaveChangesAsync();

            await OnStepSubmit.InvokeAsync(Step);
            await Modal.LetAsync(x => x.HideAsync());
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
        finally
        {
            Saving = false;
        }
    }

    private async Task MapExistingObjectsAsync(IList<DataObject> objects)
    {
        var allObjects = await GetDataObjectsAsync();
        var existing = allObjects.Where(o => objects.Any(d => d.ObjectId == Guid.Empty && o.NamesEqual(d)));
        foreach (var dbObject in existing)
        {
            var remove = objects.First(d => d.NamesEqual(dbObject));
            objects.Remove(remove);
            objects.Add(dbObject);
        }
    }

    public async Task ShowAsync(Guid stepId, StepEditModalView startView = StepEditModalView.Settings)
    {
        CurrentView = startView;
        await Modal.LetAsync(x => x.ShowAsync());
        await ResetContext();
        if (stepId != Guid.Empty)
        {
            Step = await GetExistingStepAsync(Context, stepId);
        }
        else if (stepId == Guid.Empty && Job is not null)
        {
            var job = await Context.Jobs.Include(j => j.JobParameters).FirstAsync(j => j.JobId == Job.JobId);
            Step = CreateNewStep(job);
        }
        ResetTags();
        StateHasChanged();
    }

    public void Dispose() => Context?.Dispose();
}
