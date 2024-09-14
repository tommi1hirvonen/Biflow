namespace Biflow.Ui.Shared.StepEditModal;

public abstract partial class StepEditModal<TStep> : ComponentBase, IDisposable, IStepEditModal where TStep : Step
{    
    [Inject] protected ToasterService Toaster { get; set; } = null!;
    [Inject] protected IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = null!;

    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    [Parameter] public Action? OnModalClosed { get; set; }

    [Parameter] public EventCallback<Step> OnStepSubmit { get; set; }
    
    [Parameter] public IEnumerable<MsSqlConnection> Connections { get; set; } = [];

    [Parameter] public IEnumerable<AnalysisServicesConnection> AsConnections { get; set; } = [];

    [Parameter] public IEnumerable<AppRegistration> AppRegistrations { get; set; } = [];

    [Parameter] public IEnumerable<QlikCloudClient> QlikClients { get; set; } = [];

    [Parameter] public IEnumerable<Credential> Credentials { get; set; } = [];

    internal abstract string FormId { get; }

    internal TStep? Step { get; private set; }

    internal HxModal? Modal { get; set; }

    internal StepEditModalView CurrentView { get; set; } = StepEditModalView.Settings;

    internal Dictionary<Guid, JobProjection>? JobSlims { get; set; }

    internal Dictionary<Guid, StepProjection>? StepSlims { get; set; }

    internal bool Saving { get; set; } = false;

    public IEnumerable<StepTag>? AllTags { get; private set; }

    private AppDbContext? context;
    private IEnumerable<DataObject>? dataObjects;

    protected virtual Task OnModalShownAsync(TStep step) => Task.CompletedTask;

    public async Task<IEnumerable<DataObject>> GetDataObjectsAsync()
    {
        ArgumentNullException.ThrowIfNull(context);
        dataObjects ??= await context.DataObjects.ToListAsync();
        return dataObjects;
    }

    /// <summary>
    /// Called during OnParametersSetAsync() to load an existing Step from <see cref="AppDbContext"/>.
    /// The Step loaded from the context should be tracked in order to track changes made to the object.
    /// </summary>
    /// <param name="context">Instance of <see cref="AppDbContext"/></param>
    /// <param name="stepId">Id of an existing Step that is to be edited</param>
    /// <returns></returns>
    protected abstract Task<TStep> GetExistingStepAsync(AppDbContext context, Guid stepId);

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
    protected virtual Task OnSubmitAsync(TStep step) => Task.CompletedTask;

    private async Task ResetContext()
    {
        if (context is not null)
            await context.DisposeAsync();

        context = await DbContextFactory.CreateDbContextAsync();
    }

    internal void OnClosed()
    {
        Step = null;
        AllTags = null;
        dataObjects = null;
        JobSlims = null;
        StepSlims = null;
    }

    internal async Task SubmitStepAsync()
    {
        Saving = true;

        try
        {
            ArgumentNullException.ThrowIfNull(context);
            if (Step is null)
            {
                Toaster.AddError("Error submitting step", "Step was null");
                return;
            }

            await MapExistingDataObjectsAsync(Step.DataObjects);

            await OnSubmitAsync(Step);

            // Save changes.

            // New step
            if (Step.StepId == Guid.Empty)
            {
                context.Steps.Add(Step);
            }
            // If the Step was an existing Step, the context has been tracking its changes.
            // => No need to attach it to the context separately.
            await context.SaveChangesAsync();

            await OnStepSubmit.InvokeAsync(Step);
            await Modal.LetAsync(x => x.HideAsync());
        }
        catch (DbUpdateConcurrencyException)
        {
            Toaster.AddError("Concurrency error",
                "The step has been modified outside of this session. Reload the page to view the most recent settings.");
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error adding/editing step", $"{ex.Message}\n{ex.InnerException?.Message}");
        }
        finally
        {
            Saving = false;
        }
    }

    private async Task MapExistingDataObjectsAsync(ICollection<StepDataObject> dataObjects)
    {
        var allObjects = await GetDataObjectsAsync();
        var replace = dataObjects.Where(d => d.ObjectId == Guid.Empty && allObjects.Any(o => o.UriEquals(d.DataObject))).ToArray();
        foreach (var dataObject in replace)
        {
            dataObject.DataObject = allObjects.First(o => o.UriEquals(dataObject.DataObject));
        }
    }

    public async Task ShowAsync(Guid stepId, StepEditModalView startView = StepEditModalView.Settings)
    {
        CurrentView = startView;
        await Modal.LetAsync(x => x.ShowAsync());
        await ResetContext();
        ArgumentNullException.ThrowIfNull(context);
        AllTags = await context.StepTags.OrderBy(t => t.TagName).ToListAsync();
        // Use slim classes to only load selected columns from the db.
        // When loading all steps from the db, the number of steps may be very high.
        JobSlims = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Select(j => new JobProjection(j.JobId, j.JobName, j.ExecutionMode))
            .ToDictionaryAsync(j => j.JobId);
        StepSlims = await context.Steps
            .AsNoTrackingWithIdentityResolution()
            .Select(s => new StepProjection(
                s.StepId,
                s.JobId,
                s.Job.JobName,
                s.StepName,
                s.StepType,
                s.ExecutionPhase,
                s.IsEnabled,
                s.Tags.ToArray(),
                s.Dependencies.Select(d => new DependencyProjection(d.StepId, d.DependantOnStepId, d.DependencyType)).ToArray()))
            .ToDictionaryAsync(s => s.StepId);
        if (stepId != Guid.Empty)
        {
            Step = await GetExistingStepAsync(context, stepId);
        }
        else if (stepId == Guid.Empty && Job is not null)
        {
            var job = await context.Jobs.Include(j => j.JobParameters).FirstAsync(j => j.JobId == Job.JobId);
            Step = CreateNewStep(job);
        }
        StateHasChanged();
        if (Step is not null)
            await OnModalShownAsync(Step);
    }

    public virtual void Dispose() => context?.Dispose();
}
