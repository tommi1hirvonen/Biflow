namespace Biflow.Ui.Pages;

[Route("/jobs/{Id:guid}/{DetailsPage}/{InitialStepId:guid?}")]
public partial class JobDetails : ComponentBase, IDisposable
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ToasterService Toaster { get; set; } = null!;
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;

    [Parameter] public string DetailsPage { get; set; } = "steps";

    [Parameter] public Guid Id { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly CancellationTokenSource cts = new();

    private Job? job;
    private List<Job> jobs = [];
    private List<Step> steps = [];
    private List<SqlConnectionInfo>? sqlConnections;
    private List<AnalysisServicesConnectionInfo>? asConnections;
    private List<PipelineClient>? pipelineClients;
    private List<AppRegistration>? appRegistrations;
    private List<FunctionApp>? functionApps;
    private List<QlikCloudClient>? qlikCloudClients;
    private bool descriptionOpen;

    public static IQueryable<Step> BuildStepsQueryWithIncludes(AppDbContext context)
    {
        return context.Steps
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.Tags)
            .Include(step => step.ExecutionConditionParameters)
            .ThenInclude(p => p.JobParameter)
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
            .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
            .Include(step => step.ExecutionConditionParameters);
    }

    protected override async Task OnInitializedAsync()
    {
        using var context = await DbFactory.CreateDbContextAsync();
        sqlConnections = await context.SqlConnections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionName)
            .ToListAsync(cts.Token);
        asConnections = await context.AnalysisServicesConnections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionName)
            .ToListAsync(cts.Token);
        pipelineClients = await context.PipelineClients
            .AsNoTracking()
            .OrderBy(df => df.PipelineClientName)
            .ToListAsync(cts.Token);
        appRegistrations = await context.AppRegistrations
            .AsNoTracking()
            .OrderBy(app => app.AppRegistrationName)
            .ToListAsync(cts.Token);
        functionApps = await context.FunctionApps
            .AsNoTracking()
            .OrderBy(app => app.FunctionAppName)
            .ToListAsync(cts.Token);
        qlikCloudClients = await context.QlikCloudClients
            .AsNoTracking()
            .OrderBy(c => c.QlikCloudClientName)
            .ToListAsync(cts.Token);
        job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(job => job.Category)
            .FirstAsync(job => job.JobId == Id, cts.Token);
        steps = await BuildStepsQueryWithIncludes(context)
            .Where(step => step.JobId == job.JobId)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(cts.Token);
        jobs = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .Include(j => j.Category)
            .OrderBy(j => j.JobName)
            .ToListAsync(cts.Token);
        SortSteps();
    }

    private void SortSteps()
    {
        if (job is null)
        {
            return;
        }
        try
        {
            if (job.UseDependencyMode)
            {
                var comparer = new TopologicalStepComparer(steps);
                steps.Sort(comparer);
            }
            else
            {
                steps.Sort();
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error sorting steps", ex.Message);
        }
    }

    private void OnJobUpdated(Job job)
    {
        if (this.job is null)
        {
            return;
        }
        this.job = job;
        StateHasChanged();
    }

    private async Task DeleteJob()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(job);
            using (var context1 = await DbFactory.CreateDbContextAsync())
            {
                var executingSteps = await context1.JobSteps
                    .Where(s => s.JobToExecuteId == job.JobId)
                    .Include(s => s.Job)
                    .OrderBy(s => s.Job.JobName)
                    .ThenBy(s => s.StepName)
                    .ToListAsync();
                if (executingSteps.Count != 0)
                {
                    var steps = string.Join("\n", executingSteps.Select(s => $"– {s.Job.JobName} – {s.StepName}"));
                    var message = $"""
                    This job has one or more referencing steps that execute this job:
                    {steps}
                    Removing the job will also remove these steps. Delete anyway?
                    """;
                    var confirmResult = await Confirmer.ConfirmAsync("", message);
                    if (!confirmResult)
                    {
                        return;
                    }
                }
            }
            await Mediator.SendAsync(new DeleteJobCommand(job.JobId));
            NavigationManager.NavigateTo("jobs");
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error deleting job", ex.Message);
        }
    }

    private async Task ToggleJobEnabled(ChangeEventArgs args)
    {
        try
        {
            var enabled = (bool)args.Value!;
            ArgumentNullException.ThrowIfNull(job);
            await Mediator.SendAsync(new ToggleJobCommand(job.JobId, enabled));
            job.IsEnabled = enabled;
            var message = job.IsEnabled ? "Job enabled successfully" : "Job disabled successfully";
            Toaster.AddSuccess(message);
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error toggling job", ex.Message);
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}