namespace Biflow.Ui.Shared.StepEditModal;

public abstract class StepEditModal<TStep>(
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory) : ComponentBase, IStepEditModal where TStep : Step
{
    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    [Parameter] public Action? OnModalClosed { get; set; }

    [Parameter] public EventCallback<Step> OnStepSubmit { get; set; }
    
    [Parameter] public IEnumerable<SqlConnectionBase> SqlConnections { get; set; } = [];

    [Parameter] public IEnumerable<MsSqlConnection> MsSqlConnections { get; set; } = [];

    [Parameter] public IEnumerable<AnalysisServicesConnection> AsConnections { get; set; } = [];

    [Parameter] public IEnumerable<AzureCredential> AzureCredentials { get; set; } = [];

    [Parameter] public IEnumerable<QlikCloudEnvironment> QlikClients { get; set; } = [];

    [Parameter] public IEnumerable<Credential> Credentials { get; set; } = [];

    internal abstract string FormId { get; }

    internal TStep? Step { get; private set; }

    internal HxModal? Modal { get; set; }

    internal StepEditModalView CurrentView { get; set; } = StepEditModalView.Settings;

    internal Dictionary<Guid, JobProjection>? JobSlims { get; set; }

    internal Dictionary<Guid, StepProjection>? StepSlims { get; set; }

    internal bool Saving { get; set; }

    public List<StepTag>? AllTags { get; private set; }

    protected IMediator Mediator { get; } = mediator;

    protected ToasterService Toaster { get; } = toaster;

    protected IDbContextFactory<AppDbContext> DbContextFactory { get; } = dbContextFactory;

    private IEnumerable<DataObject>? _dataObjects;

    protected virtual Task OnModalShownAsync(TStep step) => Task.CompletedTask;

    public async Task<IEnumerable<DataObject>> GetDataObjectsAsync()
    {
        if (_dataObjects is not null)
        {
            return _dataObjects;
        }
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        _dataObjects = await dbContext.DataObjects
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();
        return _dataObjects;
    }

    /// <summary>
    /// Called during OnParametersSetAsync() to load an existing Step from <see cref="AppDbContext"/>.
    /// The Step loaded from the context should be tracked in order to track changes made to the object.
    /// </summary>
    /// <param name="context">Instance of <see cref="AppDbContext"/></param>
    /// <param name="stepId">ID of an existing Step that is to be edited</param>
    /// <returns></returns>
    protected abstract Task<TStep> GetExistingStepAsync(AppDbContext context, Guid stepId);

    /// <summary>
    /// Called during OnParametersSetAsync() if a new Step is being created.
    /// The method should return an "empty" instance of Step with its navigation properties correctly initialized.
    /// </summary>
    /// <param name="job">The job in which the Step is created</param>
    /// <returns></returns>
    protected abstract TStep CreateNewStep(Job job);

    protected abstract Task<TStep> OnSubmitCreateAsync(TStep step);

    protected abstract Task<TStep> OnSubmitUpdateAsync(TStep step);

    internal void OnClosed()
    {
        Step = null;
        AllTags = null;
        _dataObjects = null;
        JobSlims = null;
        StepSlims = null;
    }

    internal async Task SubmitStepAsync()
    {
        Saving = true;

        try
        {
            if (Step is null)
            {
                Toaster.AddError("Error submitting step", "Step was null");
                return;
            }

            await AddNewDataObjectsAsync(Step.DataObjects);

            var step = Step.StepId == Guid.Empty
                ? await OnSubmitCreateAsync(Step)
                : await OnSubmitUpdateAsync(Step);

            await OnStepSubmit.InvokeAsync(step);
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

    private async Task AddNewDataObjectsAsync(ICollection<StepDataObject> dataObjects)
    {
        var newDataObjects = dataObjects
            .Where(x => x.DataObject.ObjectId == Guid.Empty)
            .GroupBy(x => x.DataObject)
            .Select(g => (g.Key, g.ToList()));
        foreach (var (newDataObject, stepDataObjects) in newDataObjects)
        {
            var command = new CreateDataObjectCommand(newDataObject.ObjectUri, 1);
            var dataObject = await Mediator.SendAsync(command);
            foreach (var stepDataObject in stepDataObjects)
            {
                stepDataObject.DataObject = dataObject;
            }
        }
    }

    public async Task ShowAsync(Guid stepId, StepEditModalView startView = StepEditModalView.Settings)
    {
        CurrentView = startView;
        await Modal.LetAsync(x => x.ShowAsync());
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        AllTags = await dbContext.StepTags
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();
        AllTags.Sort();
        // Use slim classes to only load selected columns from the db.
        // When loading all steps from the db, the number of steps may be very high.
        JobSlims = await dbContext.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Select(j => new JobProjection(j.JobId, j.JobName, j.ExecutionMode))
            .ToDictionaryAsync(j => j.JobId);
        StepSlims = await dbContext.Steps
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
            Step = await GetExistingStepAsync(dbContext, stepId);
        }
        else if (stepId == Guid.Empty && Job is not null)
        {
            var job = await dbContext.Jobs
                .AsNoTrackingWithIdentityResolution()
                .Include(j => j.JobParameters)
                .FirstAsync(j => j.JobId == Job.JobId);
            Step = CreateNewStep(job);
        }
        StateHasChanged();
        if (Step is not null)
        {
            await OnModalShownAsync(Step);
        }
    }
    
}
