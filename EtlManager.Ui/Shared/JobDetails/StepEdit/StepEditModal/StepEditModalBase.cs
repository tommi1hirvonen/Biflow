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

    [Parameter] public Action? OnModalClosed { get; set; }

    [Parameter] public EventCallback<Step> OnStepSubmit { get; set; }
    
    [Parameter] public IEnumerable<SqlConnectionInfo> Connections { get; set; } = Enumerable.Empty<SqlConnectionInfo>();

    internal abstract string FormId { get; }

    internal TStep Step { get; private set; } = null!;

    internal List<string> Tags { get; set; } = new();

    internal HxModal Modal { get; set; } = null!;

    internal string StepError { get; private set; } = string.Empty;

    internal StepEditModalView CurrentView { get; set; } = StepEditModalView.Settings;

    private EtlManagerContext Context { get; set; } = null!;

    private IEnumerable<Tag>? AllTags { get; set; }

    private IEnumerable<SourceTargetObject>? SourceTargetObjects { get; set; }

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

    public async Task<IEnumerable<SourceTargetObject>> GetSourceTargetObjectsAsync()
    {
        SourceTargetObjects ??= await Context.SourceTargetObjects.ToListAsync();
        return SourceTargetObjects;
    }

    /// <summary>
    /// Called if the edits made by the user are canceled.
    /// If there were entities added to the Step's navigation properties, for example, they should be removed.
    /// </summary>
    /// <param name="entityEntry"></param>
    protected virtual void ResetAddedEntities(EntityEntry entityEntry) { }

    /// <summary>
    /// Called if the edits made by the user are canceled.
    /// If there were entities removed from the Step's navigation properties, for example, they should be added back.
    /// </summary>
    /// <param name="entityEntry"></param>
    protected virtual void ResetDeletedEntities(EntityEntry entityEntry) { }

    protected virtual (bool Result, string? ErrorMessage) StepValidityCheck(Step step) => (true, null);

    /// <summary>
    /// Called during OnParametersSetAsync() to load an existing Step from EtlManagerContext.
    /// The Step loaded from the context should be tracked in order to track changes made to the object.
    /// </summary>
    /// <param name="context">Instance of EtlManagerContext</param>
    /// <param name="stepId">Id of an existing Step that is to be edited</param>
    /// <returns></returns>
    protected abstract Task<TStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId);

    /// <summary>
    /// Called during OnParametersSetAsync() if a new Step is being created.
    /// The method should return an "empty" instance of Step with its navigation properties correctly initialized.
    /// </summary>
    /// <param name="job">The job in which the Step is created</param>
    /// <returns></returns>
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
        else if (StepId == Guid.Empty && StepId != PrevStepId && Job is not null)
        {
            await ResetContext();
            Step = CreateNewStep(Job);
            ResetTags();
            PrevStepId = StepId;
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
        // Force the step to be reloaded and the context
        // to be recreated when the modal is opened again.
        StepId = Guid.Empty;
        AllTags = null;
        OnModalClosed?.Invoke();
    }

    internal async Task SubmitStep()
    {
        StepError = string.Empty;

        var (result, message) = StepValidityCheck(Step);
        if (!result)
        {
            StepError = message ?? string.Empty;
            return;
        }

        // Check source and target database objects.
        var (result2, message2) = await CheckSourcesAndTargetsAsync();
        if (!result2)
        {
            StepError = message2 ?? string.Empty;
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
            // New step
            if (Step.StepId == Guid.Empty)
            {
                Context.Steps.Add(Step);
            }
            // If the Step was an existing Step, the context has been tracking its changes.
            // => No need to attach it to the context separately.
            await Context.SaveChangesAsync();

            await OnStepSubmit.InvokeAsync(Step);
            await Modal.HideAsync();

            StepId = Guid.Empty;
            AllTags = null;
            SourceTargetObjects = null;
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

    private async Task<(bool Result, string? Message)> CheckSourcesAndTargetsAsync()
    {
        var sources = Step.Sources
            .OrderBy(x => x.ServerName)
            .ThenBy(x => x.DatabaseName)
            .ThenBy(x => x.SchemaName)
            .ThenBy(x => x.ObjectName)
            .ToList();
        if (!CheckSourceTargetDuplicates(sources)) return (false, "Duplicate sources");
        
        if (sources.Any(x => !x.ServerName.Any()
        || !x.DatabaseName.Any()
        || !x.SchemaName.Any()
        || !x.ObjectName.Any()))
            return (false, "Empty source object names are not valid");
        
        var targets = Step.Targets
            .OrderBy(x => x.ServerName)
            .ThenBy(x => x.DatabaseName)
            .ThenBy(x => x.SchemaName)
            .ThenBy(x => x.ObjectName)
            .ToList();
        if (!CheckSourceTargetDuplicates(targets)) return (false, "Duplicate targets");

        if (targets.Any(x => !x.ServerName.Any()
        || !x.DatabaseName.Any()
        || !x.SchemaName.Any()
        || !x.ObjectName.Any()))
            return (false, "Empty target object names are not valid");

        await MapExistingObjectsAsync(Step.Sources);
        await MapExistingObjectsAsync(Step.Targets);

        return (true, null);
    }

    private static bool CheckSourceTargetDuplicates(IEnumerable<SourceTargetObject> objects)
    {
        for (int i = 0; i < objects.Count() - 1; i++)
        {
            var current = objects.ElementAt(i);
            var next = objects.ElementAt(i + 1);
            if (current.Equals(next))
            {
                return false;
            }
        }
        return true;
    }

    private async Task MapExistingObjectsAsync(IList<SourceTargetObject> objects)
    {
        var allObjects = await GetSourceTargetObjectsAsync();
        var existing = allObjects.Where(o => objects.Any(d => d.ObjectId == Guid.Empty && o.Equals(d)));
        foreach (var dbObject in existing)
        {
            var remove = objects.First(d => d.Equals(dbObject));
            objects.Remove(remove);
            objects.Add(dbObject);
        }
    }

    public async Task ShowAsync(StepEditModalView startView = StepEditModalView.Settings)
    {
        CurrentView = startView;
        await Modal.ShowAsync();
    }

    public void Dispose() => Context?.Dispose();
}
