using Microsoft.JSInterop;
using System.Text.Json;

namespace Biflow.Ui.Pages;

[Route("/jobs/{Id:guid}/{DetailsPage}/{InitialStepId:guid?}")]
public partial class JobDetails(
    IDbContextFactory<AppDbContext> dbContextFactory,
    NavigationManager navigationManager,
    ToasterService toaster,
    IHxMessageBoxService confirmer,
    IMediator mediator,
    IJSRuntime js) : ComponentBase, IDisposable
{
    [Parameter] public string DetailsPage { get; set; } = "steps";

    [Parameter] public Guid Id { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly NavigationManager _navigationManager = navigationManager;
    private readonly ToasterService _toaster = toaster;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly IMediator _mediator = mediator;
    private readonly IJSRuntime _js = js;
    private readonly CancellationTokenSource cts = new();
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    private Job? job;
    private List<Job> jobs = [];
    private List<Step> steps = [];
    private List<ConnectionBase>? sqlConnections;
    private List<MsSqlConnection>? msSqlConnections;
    private List<AnalysisServicesConnection>? asConnections;
    private List<PipelineClient>? pipelineClients;
    private List<AppRegistration>? appRegistrations;
    private List<FunctionApp>? functionApps;
    private List<QlikCloudEnvironment>? qlikCloudClients;
    private List<DatabricksWorkspace>? databricksWorkspaces;
    private List<Credential>? credentials;
    private bool descriptionOpen;
    private Guid previousJobId;

    protected override async Task OnParametersSetAsync()
    {
        if (Id != previousJobId)
        {
            previousJobId = Id;
            await OnInitializedAsync();
        }
    }

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
        using var context = await _dbContextFactory.CreateDbContextAsync();
        sqlConnections = await context.Connections
            .AsNoTracking()
            .Where(c => c.ConnectionType == ConnectionType.Sql || c.ConnectionType == ConnectionType.Snowflake)
            .OrderBy(c => c.ConnectionName)
            .ToListAsync(cts.Token);
        msSqlConnections = sqlConnections.OfType<MsSqlConnection>().ToList();
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
        qlikCloudClients = await context.QlikCloudEnvironments
            .AsNoTracking()
            .OrderBy(c => c.QlikCloudEnvironmentName)
            .ToListAsync(cts.Token);
        databricksWorkspaces = await context.DatabricksWorkspaces
            .AsNoTracking()
            .OrderBy(w => w.WorkspaceName)
            .ToListAsync(cts.Token);
        credentials = await context.Credentials
            .AsNoTracking()
            .OrderBy(c => c.Domain)
            .ThenBy(c => c.Username)
            .ToListAsync(cts.Token);
        job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .FirstAsync(job => job.JobId == Id, cts.Token);
        steps = await BuildStepsQueryWithIncludes(context)
            .Where(step => step.JobId == job.JobId)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(cts.Token);
        jobs = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
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
            if (job.ExecutionMode == ExecutionMode.Dependency)
            {
                var comparer = new TopologicalStepComparer(steps);
                steps.Sort(comparer);
            }
            else if (job.ExecutionMode == ExecutionMode.Hybrid)
            {
                var topologicalComparer = new TopologicalStepComparer(steps);
                var comparer = Comparer<Step>.Create((s1, s2) =>
                {
                    var result = s1.ExecutionPhase.CompareTo(s2.ExecutionPhase);
                    return result != 0 ? result : topologicalComparer.Compare(s1, s2);
                });
                steps.Sort(comparer);
            }
            else
            {
                steps.Sort();
            }
            StateHasChanged();
        }
        catch (CyclicDependencyException<Step> ex)
        {
            var cycles = ex.CyclicObjects.Select(c => c.Select(s => new { s.StepId, s.StepName, s.StepType }));
            var message = JsonSerializer.Serialize(cycles, jsonOptions);
            _ = _js.InvokeVoidAsync("console.log", message).AsTask();
            _toaster.AddError("Error sorting steps", "Cyclic dependencies detected. See browser console for detailed output.");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error sorting steps", ex.Message);
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
            using (var context1 = await _dbContextFactory.CreateDbContextAsync())
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
                    var confirmResult = await _confirmer.ConfirmAsync("", message);
                    if (!confirmResult)
                    {
                        return;
                    }
                }
            }
            await _mediator.SendAsync(new DeleteJobCommand(job.JobId));
            _navigationManager.NavigateTo("jobs");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error deleting job", ex.Message);
        }
    }

    private async Task ToggleJobEnabled(ChangeEventArgs args)
    {
        try
        {
            var enabled = (bool)args.Value!;
            ArgumentNullException.ThrowIfNull(job);
            await _mediator.SendAsync(new ToggleJobCommand(job.JobId, enabled));
            job.IsEnabled = enabled;
            var message = job.IsEnabled ? "Job enabled successfully" : "Job disabled successfully";
            _toaster.AddSuccess(message);
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error toggling job", ex.Message);
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}