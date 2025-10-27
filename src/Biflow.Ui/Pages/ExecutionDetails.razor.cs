using Biflow.Ui.Shared;
using Biflow.Ui.Shared.Executions;

namespace Biflow.Ui.Pages;

[Route("/executions/{ExecutionId:guid}/{Page}/{InitialStepId:guid?}")]
public partial class ExecutionDetails(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ToasterService toaster,
    IExecutorService executorService,
    NavigationManager navigationManager,
    IHxMessageBoxService confirmer,
    IMediator mediator) : ComponentBase, IDisposable
{
    [CascadingParameter] public Task<AuthenticationState>? AuthenticationState { get; set; }

    [Parameter] public Guid ExecutionId { get; set; }

    [Parameter] public string Page { get; set; } = "list";

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
    private readonly ToasterService _toaster = toaster;
    private readonly IExecutorService _executorService = executorService;
    private readonly NavigationManager _navigationManager = navigationManager;
    private readonly IHxMessageBoxService _confirmer = confirmer;
    private readonly IMediator _mediator = mediator;
    private readonly CancellationTokenSource _cts = new();

    private const int TimerIntervalSeconds = 10;
    private readonly System.Timers.Timer _timer = new(TimeSpan.FromSeconds(TimerIntervalSeconds)) { AutoReset = false };
    private readonly HashSet<StepType> _stepTypeFilter = [];
    private readonly HashSet<TagProjection> _tagFilter = [];
    private readonly HashSet<StepExecutionStatus> _stepStatusFilter = [];
    private readonly HashSet<(string StepName, StepType StepType)> _stepFilter = [];
    private FilterDropdownMode _tagFilterMode = FilterDropdownMode.Any;
    private Guid _prevExecutionId;
    private ExecutionDetailsProjection? _execution;
    private StepExecutionDetailsProjection[]? _steps;
    private ExecutionParameter[]? _executionParameters;
    private Job? _job;
    private Schedule? _schedule;
    private bool _loading;
    private StepExecutionSortMode _sortMode = StepExecutionSortMode.StartedAsc;
    private bool _showStepTags;
    private StepExecutionsTable? _stepExecutionsTable;
    private StepExecutionsGraph? _stepExecutionsGraph;
    private ExecutionParameterLineageOffcanvas? _parameterLineageOffcanvas;
    private ExecutionDependenciesGraph? _dependenciesGraph;
    // Cache dependency graph step executions here in the parent component.
    // This means they don't have to be reloaded from the database every time the graph is shown.
    private StepExecution[]? _dependenciesGraphStepExecutions;

    // Maintain a list of executions that are being stopped.
    // This same component instance can be used to switch between different job executions.
    // This list allows for stopping multiple executions concurrently
    // and to modify the view based on which job execution is being shown.
    private readonly List<Guid> _stoppingExecutions = [];

    private bool AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            _autoRefresh = value;
            _timer.Stop();
            if (_autoRefresh)
            {
                _timer.Start();
            }
        }
    }

    // TODO Replace with field keyword in .NET 10
    private bool _autoRefresh = true;

    private bool Stopping => _stoppingExecutions.Any(id => id == ExecutionId);

    private IEnumerable<StepExecutionDetailsProjection>? GetOrderedExecutions()
    {
        var filtered = _steps
            ?.Where(e =>
            (_tagFilterMode is FilterDropdownMode.Any && (_tagFilter.Count == 0 || _tagFilter.Any(tag => e.StepTags.Any(t => t.TagName == tag.TagName))))
            || (_tagFilterMode is FilterDropdownMode.All && _tagFilter.All(tag => e.StepTags.Any(t => t.TagName == tag.TagName))))
            .Where(e => _stepStatusFilter.Count == 0 || _stepStatusFilter.Contains(e.StepExecutionStatus))
            .Where(e => _stepFilter.Count == 0 || _stepFilter.Contains((e.StepName, e.StepType)))
            .Where(e => _stepTypeFilter.Count == 0 || _stepTypeFilter.Contains(e.StepType));
        return _sortMode switch
        {
            StepExecutionSortMode.StepAsc => filtered?.OrderBy(e => e.StepName),
            StepExecutionSortMode.StepDesc => filtered?.OrderByDescending(e => e.StepName),
            StepExecutionSortMode.StartedAsc => filtered?.OrderBy(e => e.StartedOn is null).ThenBy(e => e.StartedOn).ThenBy(e => e.ExecutionPhase),
            StepExecutionSortMode.StartedDesc => filtered?.OrderByDescending(e => e.StartedOn).ThenByDescending(e => e.ExecutionPhase),
            StepExecutionSortMode.EndedAsc => filtered?.OrderBy(e => e.EndedOn),
            StepExecutionSortMode.EndedDesc => filtered?.OrderByDescending(e => e.EndedOn),
            StepExecutionSortMode.DurationAsc => filtered?.OrderBy(e => e.ExecutionInSeconds).ThenByDescending(e => e.StartedOn),
            StepExecutionSortMode.DurationDesc => filtered?.OrderByDescending(e => e.ExecutionInSeconds).ThenByDescending(e => e.StartedOn),
            _ => filtered?.OrderBy(e => e.StartedOn is null).ThenBy(e => e.StartedOn).ThenBy(e => e.ExecutionPhase)
        };
    }

    private Report ShowReport => Page switch
    {
        "gantt" => Report.Gantt,
        "graph" => Report.Graph,
        "executiondetails" => Report.ExecutionDetails,
        "parameters" => Report.Parameters,
        "rerun" => Report.Rerun,
        "history" => Report.History,
        _ => Report.List
    };

    private enum Report { List, Gantt, Graph, ExecutionDetails, Parameters, Rerun, History }

    protected override void OnInitialized()
    {
        _timer.Elapsed += async (_, _) =>
        {
            if (!AutoRefresh)
            {
                return;
            }
            if (ShowStatusBar)
            {
                await LoadData();
            }
            else
            {
                _timer.Stop();
                _timer.Start();
            }
        };
    }

    private bool ShowStatusBar => ShowReport switch
    {
        Report.List or Report.Gantt or Report.Graph or Report.ExecutionDetails or Report.Parameters => true,
        _ => false
    };

    protected override async Task OnParametersSetAsync()
    {
        if (ExecutionId == _prevExecutionId)
        {
            return;
        }
        _prevExecutionId = ExecutionId;
        ClearFilters();
        _sortMode = StepExecutionSortMode.StartedAsc;
        AutoRefresh = true;
        await LoadData();
    }

    private async Task LoadData()
    {
        if (ExecutionId != Guid.Empty)
        {
            _timer.Stop();
            _loading = true;
            await InvokeAsync(StateHasChanged);
            
            Task? graphTask;
            if (ShowReport == Report.Graph)
            {
                // Graph is being shown, and data is being reloaded => force graph reload.
                // This will also update the graph step executions array.
                graphTask = _dependenciesGraph?.LoadDataAndGraphAsync(
                    forceReload: true,
                    cancellationToken: _cts.Token);
            }
            else
            {
                // Data is being reloaded, but the graph is not currently shown.
                // Reset the graph step executions array so that the next time the graph is shown,
                // the steps will be reloaded then.
                _dependenciesGraphStepExecutions = null;
                graphTask = null;
            }
            
            await using var context = await _dbContextFactory.CreateDbContextAsync();

            try
            {
                _execution = await (
                    from e in context.Executions.AsNoTracking()
                    join job in context.Jobs on e.JobId equals job.JobId into ej
                    from job in ej.DefaultIfEmpty() // Translates to left join in SQL
                    where e.ExecutionId == ExecutionId
                    orderby e.CreatedOn descending, e.StartedOn descending
                    select new ExecutionDetailsProjection(
                        e.ExecutionId,
                        e.JobId,
                        job.JobName ?? e.JobName,
                        e.ScheduleId,
                        e.ScheduleName,
                        e.CronExpression,
                        e.CreatedBy,
                        e.CreatedOn,
                        e.StartedOn,
                        e.EndedOn,
                        e.ExecutionMode,
                        e.ExecutionStatus,
                        e.ExecutorProcessId,
                        e.StopOnFirstError,
                        e.MaxParallelSteps,
                        e.TimeoutMinutes,
                        e.OvertimeNotificationLimitMinutes,
                        e.ParentExecution
                    )).FirstOrDefaultWithNoLockAsync(_cts.Token);
                
                _steps = await (
                    from e in context.StepExecutionAttempts.AsNoTracking()
                    join step in context.Steps on e.StepId equals step.StepId into s
                    from step in s.DefaultIfEmpty()
                    where e.ExecutionId == ExecutionId
                    orderby e.StepExecution.Execution.CreatedOn descending,
                        e.StartedOn descending,
                        e.StepExecution.ExecutionPhase descending
                    select new StepExecutionDetailsProjection(
                        e.ExecutionId,
                        e.StepExecution.StepId,
                        e.RetryAttemptIndex,
                        step.StepName ?? e.StepExecution.StepName,
                        e.StepType,
                        e.StepExecution.ExecutionPhase,
                        e.StartedOn,
                        e.EndedOn,
                        e.ExecutionStatus,
                        e.StepExecution.Execution.ExecutionStatus,
                        e.StepExecution.Execution.ExecutionMode,
                        e.StepExecution.Execution.JobName,
                        step.Dependencies.Select(d => d.DependantOnStepId).ToArray(),
                        step.Tags.Select(t => new TagProjection(t.TagId, t.TagName, t.Color, t.SortOrder)).ToArray()
                    )).ToArrayWithNoLockAsync(_cts.Token);
                
                await InvokeAsync(StateHasChanged);
                
                // Load additional data after calling StateHasChanged().
                // This could take some time, and with the data loaded until now,
                // most of the UI elements can already be rendered.
                _executionParameters = await context.Set<ExecutionParameter>()
                    .Where(p => p.ExecutionId == ExecutionId)
                    .OrderBy(p => p.ParameterName)
                    .ToArrayWithNoLockAsync(_cts.Token);
                
                _job = _execution is not null
                    ? await context.Jobs
                        .AsNoTrackingWithIdentityResolution()
                        .FirstOrDefaultAsync(j => j.JobId == _execution.JobId, _cts.Token)
                    : null;
                _schedule = _execution?.ScheduleId is not null
                    ? await context.Schedules
                        .AsNoTrackingWithIdentityResolution()
                        .FirstOrDefaultAsync(s => s.ScheduleId == _execution.ScheduleId, _cts.Token)
                    : null;

                if (_stepExecutionsTable is not null)
                    await _stepExecutionsTable.RefreshSelectedStepExecutionAsync();
                
                if (_stepExecutionsGraph is not null)
                    await _stepExecutionsGraph.RefreshSelectedStepExecutionAsync();
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Finally, await the dependency graph task if it was set (graph is being shown).
            if (graphTask is not null)
            {
                await graphTask;
            }
            _loading = false;
            
            // If AutoRefresh is enabled, and the execution is either running
            // or it was created less than a minute ago and hasn't yet been started.
            if (AutoRefresh 
                && (_execution?.ExecutionStatus == ExecutionStatus.Running
                    || _execution?.ExecutionStatus == ExecutionStatus.NotStarted
                    && _execution?.CreatedOn >= DateTimeOffset.Now.AddMinutes(-1)))
            {
                _timer.Start();
            }
            else
            {
                AutoRefresh = false;
            }
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task StopJobExecutionAsync()
    {
        if (!await _confirmer.ConfirmAsync("Stop execution", "Are you sure you want to stop all running steps in this execution?"))
        {
            return;
        }

        if (Stopping)
        {
            _toaster.AddInformation("Execution is already stopping");
            return;
        }

        if (_execution is null)
        {
            _toaster.AddError("Execution was null");
            return;
        }

        _stoppingExecutions.Add(ExecutionId);
        try
        {
            ArgumentNullException.ThrowIfNull(AuthenticationState);
            var authState = await AuthenticationState;
            var username = authState.User.Identity?.Name;
            ArgumentNullException.ThrowIfNull(username);

            await _executorService.StopExecutionAsync(_execution.ExecutionId, username);
            _toaster.AddSuccess("Stop request sent successfully to the executor service");
        }
        catch (TimeoutException)
        {
            _toaster.AddError("Operation timed out", "The executor process may no longer be running");
            _stoppingExecutions.RemoveAll(id => id == ExecutionId);
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error stopping execution", ex.Message);
            _stoppingExecutions.RemoveAll(id => id == ExecutionId);
        }
    }

    private async Task UpdateExecutionStatusAsync(ExecutionStatus status)
    {
        try
        {
            if (_execution is not null)
            {
                _execution = _execution with
                {
                    ExecutionStatus = status,
                    StartedOn = _execution.StartedOn ?? DateTimeOffset.Now,
                    EndedOn = _execution.EndedOn ?? DateTimeOffset.Now
                };
                var command = new UpdateExecutionStatusCommand([_execution.ExecutionId], status);
                await _mediator.SendAsync(command);
            }
            _toaster.AddSuccess("Status updated successfully");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error updating status", ex.Message);
        }
    }

    private async Task DeleteExecutionAsync()
    {
        if (!await _confirmer.ConfirmAsync("Delete execution?", "Deleting executions that might be running can lead to undefined behaviour of the executor service. Are you sure you want to delete this execution?"))
        {
            return;
        }
        try
        {
            await _mediator.SendAsync(new DeleteExecutionCommand(ExecutionId));
            _navigationManager.NavigateTo("/executions");
            _toaster.AddSuccess("Execution deleted successfully");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error deleting execution", ex.Message);
        }
    }

    private void ClearFilters()
    {
        _stepStatusFilter.Clear();
        _stepFilter.Clear();
        _stepTypeFilter.Clear();
        _tagFilter.Clear();
        _tagFilterMode = FilterDropdownMode.Any;
    }
    
    private int GetProgressPercent()
    {
        if (_steps is null)
        {
            return 0;
        }
        var allCount = _steps.DistinctBy(s => s.StepId).Count();
        var completedCount = _steps.Count(s =>
            s.StepExecutionStatus is
                StepExecutionStatus.Succeeded or
                StepExecutionStatus.Warning or
                StepExecutionStatus.Failed or
                StepExecutionStatus.Stopped or
                StepExecutionStatus.Skipped or
                StepExecutionStatus.Duplicate);
        return allCount > 0
            ? (int)Math.Round(completedCount / (double)allCount * 100)
            : 0;
    }
    
    private decimal GetSuccessPercent()
    {
        if (_steps is null)
        {
            return 0;
        }
        var allCount = _steps.DistinctBy(s => s.StepId).Count();
        var successCount = _steps.Count(s =>
            s.StepExecutionStatus is StepExecutionStatus.Succeeded or StepExecutionStatus.Warning);
        return allCount > 0
            ? (decimal)successCount / allCount * 100
            : 0;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }
}
