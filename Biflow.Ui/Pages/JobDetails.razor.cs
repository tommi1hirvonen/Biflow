﻿using Microsoft.JSInterop;
using System.Text.Json;
using Havit.Linq;

namespace Biflow.Ui.Pages;

[Route("/jobs/{Id:guid}/{DetailsPage}/{InitialStepId:guid?}")]
[Route("/jobs/{Id:guid}/settings/{SettingsPage?}")]
public partial class JobDetails(
    IDbContextFactory<AppDbContext> dbContextFactory,
    NavigationManager navigationManager,
    ToasterService toaster,
    IHxMessageBoxService confirmer,
    IMediator mediator,
    IJSRuntime js) : ComponentBase, IDisposable
{
    [Parameter] public string DetailsPage { get; set; } = "steps";

    [Parameter] public string? SettingsPage { get; set; }

    [Parameter] public Guid Id { get; set; }

    [Parameter] public Guid? InitialStepId { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly NavigationManager _navigationManager = navigationManager;
    private readonly ToasterService _toaster = toaster;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly IMediator _mediator = mediator;
    private readonly IJSRuntime _js = js;
    private readonly CancellationTokenSource _cts = new();

    private Job? _job;
    private List<Job> _jobs = [];
    private List<Step> _steps = [];
    private List<SqlConnectionBase>? _sqlConnections;
    private List<MsSqlConnection>? _msSqlConnections;
    private List<AnalysisServicesConnection>? _asConnections;
    private List<PipelineClient>? _pipelineClients;
    private List<AppRegistration>? _appRegistrations;
    private List<FunctionApp>? _functionApps;
    private List<QlikCloudEnvironment>? _qlikCloudClients;
    private List<DatabricksWorkspace>? _databricksWorkspaces;
    private List<DbtAccount>? _dbtAccounts;
    private List<ScdTable>? _scdTables;
    private List<Credential>? _credentials;
    private bool _descriptionOpen;
    private Guid _previousJobId;

    protected override async Task OnParametersSetAsync()
    {
        if (Id != _previousJobId)
        {
            _previousJobId = Id;
            await OnInitializedAsync();
        }

        if (_navigationManager.Uri.EndsWith("settings") && SettingsPage is null)
        {
            _navigationManager.NavigateTo($"/jobs/{Id}/settings/general");
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
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        _sqlConnections = await context.SqlConnections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionName)
            .ToListAsync(_cts.Token);
        _msSqlConnections = _sqlConnections.OfType<MsSqlConnection>().ToList();
        _asConnections = await context.AnalysisServicesConnections
            .AsNoTracking()
            .OrderBy(c => c.ConnectionName)
            .ToListAsync(_cts.Token);
        _pipelineClients = await context.PipelineClients
            .AsNoTracking()
            .OrderBy(df => df.PipelineClientName)
            .ToListAsync(_cts.Token);
        _appRegistrations = await context.AppRegistrations
            .AsNoTracking()
            .OrderBy(app => app.AppRegistrationName)
            .ToListAsync(_cts.Token);
        _functionApps = await context.FunctionApps
            .AsNoTracking()
            .OrderBy(app => app.FunctionAppName)
            .ToListAsync(_cts.Token);
        _qlikCloudClients = await context.QlikCloudEnvironments
            .AsNoTracking()
            .OrderBy(c => c.QlikCloudEnvironmentName)
            .ToListAsync(_cts.Token);
        _databricksWorkspaces = await context.DatabricksWorkspaces
            .AsNoTracking()
            .OrderBy(w => w.WorkspaceName)
            .ToListAsync(_cts.Token);
        _dbtAccounts = await context.DbtAccounts
            .AsNoTracking()
            .OrderBy(a => a.DbtAccountName)
            .ToListAsync(_cts.Token);
        _scdTables = await context.ScdTables
            .AsNoTracking()
            .Include(t => t.Connection) // used in data objects editor in step edit modal
            .OrderBy(t => t.ScdTableName)
            .ToListAsync(_cts.Token);
        _credentials = await context.Credentials
            .AsNoTracking()
            .OrderBy(c => c.Domain)
            .ThenBy(c => c.Username)
            .ToListAsync(_cts.Token);
        _job = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .FirstAsync(j => j.JobId == Id, _cts.Token);
        _steps = await BuildStepsQueryWithIncludes(context)
            .Where(step => step.JobId == _job.JobId)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(_cts.Token);
        _jobs = await context.Jobs
            .AsNoTrackingWithIdentityResolution()
            .OrderBy(j => j.JobName)
            .ToListAsync(_cts.Token);
        SortSteps();
    }

    private void SortSteps()
    {
        if (_job is null)
        {
            return;
        }
        try
        {
            switch (_job.ExecutionMode)
            {
                case ExecutionMode.Dependency:
                {
                    var comparer = new TopologicalStepComparer(_steps);
                    _steps.Sort(comparer);
                    break;
                }
                case ExecutionMode.Hybrid:
                {
                    var topologicalComparer = new TopologicalStepComparer(_steps);
                    var comparer = Comparer<Step>.Create((s1, s2) =>
                    {
                        var result = s1.ExecutionPhase.CompareTo(s2.ExecutionPhase);
                        return result != 0 ? result : topologicalComparer.Compare(s1, s2);
                    });
                    _steps.Sort(comparer);
                    break;
                }
                case ExecutionMode.ExecutionPhase:
                default:
                    _steps.Sort();
                    break;
            }

            StateHasChanged();
        }
        catch (CyclicDependencyException<Step> ex)
        {
            var cycles = ex.CyclicObjects.Select(c => c.Select(s => new { s.StepId, s.StepName, s.StepType }));
            var message = JsonSerializer.Serialize(cycles, JsonOptions);
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
        if (_job is null)
        {
            return;
        }
        _job = job;
        StateHasChanged();
    }

    private async Task DeleteJob()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_job);
            await using (var context1 = await _dbContextFactory.CreateDbContextAsync())
            {
                var executingSteps = await context1.JobSteps
                    .Where(s => s.JobToExecuteId == _job.JobId)
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
            await _mediator.SendAsync(new DeleteJobCommand(_job.JobId));
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
            ArgumentNullException.ThrowIfNull(_job);
            await _mediator.SendAsync(new ToggleJobEnabledCommand(_job.JobId, enabled));
            _job.IsEnabled = enabled;
            var message = _job.IsEnabled ? "Job enabled successfully" : "Job disabled successfully";
            _toaster.AddSuccess(message);
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error toggling job", ex.Message);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}