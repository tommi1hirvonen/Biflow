using Biflow.Ui.Shared.Executions;
using System.Timers;

namespace Biflow.Ui.Pages;

[Route("/executions/{ExecutionId:guid}/{Page}/{InitialStepId:guid?}")]
public partial class ExecutionDetails : ComponentBase, IDisposable
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] private ToasterService Toaster { get; set; } = null!;
    [Inject] private IExecutorService ExecutorService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;

    [CascadingParameter] public Task<AuthenticationState>? AuthenticationState { get; set; }

    [Parameter] public Guid ExecutionId { get; set; }

    [Parameter] public string Page { get; set; } = "list";

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly CancellationTokenSource cts = new();

    private const int TimerIntervalSeconds = 10;
    private readonly System.Timers.Timer timer = new(TimeSpan.FromSeconds(TimerIntervalSeconds)) { AutoReset = false };
    private readonly HashSet<StepType> stepTypeFilter = [];
    private readonly HashSet<TagProjection> tagFilter = [];
    private readonly HashSet<StepExecutionStatus> stepStatusFilter = [];
    private readonly HashSet<(string StepName, StepType StepType)> stepFilter = [];
    private Guid prevExecutionId;
    private Execution? execution;
    private IEnumerable<StepExecutionProjection>? stepProjections = null;
    private Job? job;
    private Schedule? schedule;
    private bool loading = false;
    private StepExecutionSortMode sortMode = StepExecutionSortMode.StartedAsc;
    private ExecutionParameterLineageOffcanvas? parameterLineageOffcanvas;
    private ExecutionDependenciesGraph? dependenciesGraph;

    // Maintain a list of executions that are being stopped.
    // This same component instance can be used to switch between different job executions.
    // This list allows for stopping multiple executions concurrently
    // and to modify the view based on which job execution is being shown.
    private readonly List<Guid> stoppingExecutions = [];

    private bool AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            _autoRefresh = value;
            timer.Stop();
            if (_autoRefresh)
            {
                timer.Start();
            }
        }
    }

    private bool _autoRefresh = true;

    private bool Stopping => stoppingExecutions.Any(id => id == ExecutionId);

    private IEnumerable<StepExecutionProjection>? GetOrderedExecutions()
    {
        var filtered = stepProjections
            ?.Where(e => tagFilter.Count == 0 || e.StepTags.Any(t1 => tagFilter.Any(t2 => t1.TagId == t2.TagId)) == true)
            .Where(e => stepStatusFilter.Count == 0 || stepStatusFilter.Contains(e.StepExecutionStatus))
            .Where(e => stepFilter.Count == 0 || stepFilter.Contains((e.StepName, e.StepType)))
            .Where(e => stepTypeFilter.Count == 0 || stepTypeFilter.Contains(e.StepType));
        return sortMode switch
        {
            StepExecutionSortMode.StepAsc => filtered?.OrderBy(e => e.StepName),
            StepExecutionSortMode.StepDesc => filtered?.OrderByDescending(e => e.StepName),
            StepExecutionSortMode.StartedAsc => filtered?.OrderBy(e => e.StartedOn is null).ThenBy(e => e.StartedOn),
            StepExecutionSortMode.StartedDesc => filtered?.OrderByDescending(e => e.StartedOn),
            StepExecutionSortMode.EndedAsc => filtered?.OrderBy(e => e.EndedOn),
            StepExecutionSortMode.EndedDesc => filtered?.OrderByDescending(e => e.EndedOn),
            StepExecutionSortMode.DurationAsc => filtered?.OrderBy(e => e.ExecutionInSeconds).ThenByDescending(e => e.StartedOn),
            StepExecutionSortMode.DurationDesc => filtered?.OrderByDescending(e => e.ExecutionInSeconds).ThenByDescending(e => e.StartedOn),
            _ => filtered?.OrderBy(e => e.StartedOn is null).ThenBy(e => e.StartedOn)
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
        timer.Elapsed += async (object? source, ElapsedEventArgs e) =>
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
                timer.Stop();
                timer.Start();
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
        if (ExecutionId == prevExecutionId)
        {
            return;
        }
        prevExecutionId = ExecutionId;
        await LoadData();
    }

    private async Task LoadData()
    {
        if (ExecutionId != Guid.Empty)
        {
            timer.Stop();
            loading = true;
            await InvokeAsync(StateHasChanged);
            using var context = DbFactory.CreateDbContext();

            try
            {
                var stepExecutions = await (
                from exec in context.StepExecutions
                    .AsNoTrackingWithIdentityResolution()
                    .Where(e => e.ExecutionId == ExecutionId)
                    .Include(e => e.Execution).ThenInclude(e => e.ExecutionParameters)
                    .Include(e => e.StepExecutionAttempts)
                    .Include(e => e.ExecutionDependencies)
                    .Include(e => e.MonitoredStepExecutions).ThenInclude(e => e.MonitoredStepExecution).ThenInclude(e => e.StepExecutionAttempts)
                    .Include($"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
                    .Include($"{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                    .Include(e => e.ExecutionConditionParameters).ThenInclude(p => p.ExecutionParameter)
                join step in context.Steps.Include(s => s.Tags) on exec.StepId equals step.StepId into es
                from step in es.DefaultIfEmpty()
                select new { exec, step }
                ).ToArrayAsync(cts.Token);
                foreach (var item in stepExecutions)
                {
                    item.exec.SetStep(item.step);
                }
                execution = stepExecutions.FirstOrDefault()?.exec.Execution;
                job = execution is not null
                    ? await context.Jobs.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(j => j.JobId == execution.JobId, cts.Token)
                    : null;
                schedule = execution?.ScheduleId is not null
                    ? await context.Schedules.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(s => s.ScheduleId == execution.ScheduleId, cts.Token)
                    : null;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            stepProjections = execution?.StepExecutions
                .SelectMany(e => e.StepExecutionAttempts)
                .Select(e => new StepExecutionProjection(
                    e.StepExecution.ExecutionId,
                    e.StepExecution.StepId,
                    e.RetryAttemptIndex,
                    e.StepExecution.GetStep()?.StepName ?? e.StepExecution.StepName,
                    e.StepType,
                    e.StepExecution.ExecutionPhase,
                    e.StepExecution.Execution.CreatedOn,
                    e.StartedOn,
                    e.EndedOn,
                    e.ExecutionStatus,
                    e.StepExecution.Execution.ExecutionStatus,
                    e.StepExecution.Execution.ExecutionMode,
                    e.StepExecution.Execution.ScheduleId,
                    e.StepExecution.Execution.ScheduleName,
                    e.StepExecution.Execution.JobId,
                    job?.JobName ?? e.StepExecution.Execution.JobName,
                    e.StepExecution.ExecutionDependencies.Select(d => d.DependantOnStepId).ToArray(),
                    e.StepExecution.GetStep()?.Tags.Select(t => new TagProjection(t.TagId, t.TagName, t.Color)).ToArray() ?? [],
                    []))
                .ToArray();

            loading = false;
            if (AutoRefresh && (execution?.ExecutionStatus == ExecutionStatus.Running || execution?.ExecutionStatus == ExecutionStatus.NotStarted))
            {
                timer.Start();
            }
            else
            {
                AutoRefresh = false;
            }
            await InvokeAsync(StateHasChanged);
            if (ShowReport == Report.Graph && dependenciesGraph is not null)
            {
                await InvokeAsync(dependenciesGraph.LoadGraphAsync);
            }
        }
    }

    private async Task StopJobExecutionAsync()
    {
        if (!await Confirmer.ConfirmAsync("Stop execution", $"Are you sure you want to stop all running steps in this execution?"))
        {
            return;
        }

        if (Stopping)
        {
            Toaster.AddInformation("Execution is already stopping");
            return;
        }

        if (execution is null)
        {
            Toaster.AddError("Execution was null");
            return;
        }

        stoppingExecutions.Add(ExecutionId);
        try
        {
            ArgumentNullException.ThrowIfNull(AuthenticationState);
            var authState = await AuthenticationState;
            var username = authState.User.Identity?.Name;
            ArgumentNullException.ThrowIfNull(username);

            await ExecutorService.StopExecutionAsync(execution.ExecutionId, username);
            Toaster.AddSuccess("Stop request sent successfully to the executor service");
        }
        catch (TimeoutException)
        {
            Toaster.AddError("Operation timed out", "The executor process may no longer be running");
            stoppingExecutions.RemoveAll(id => id == ExecutionId);
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error stopping execution", ex.Message);
            stoppingExecutions.RemoveAll(id => id == ExecutionId);
        }
    }

    private async Task UpdateExecutionStatusAsync(ExecutionStatus status)
    {
        try
        {
            if (execution is not null)
            {
                execution.ExecutionStatus = status;
                execution.StartedOn ??= DateTimeOffset.Now;
                execution.EndedOn ??= DateTimeOffset.Now;
                await Mediator.SendAsync(new UpdateExecutionCommand(execution));
            }
            Toaster.AddSuccess("Status updated successfully");
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error updating status", ex.Message);
        }
    }

    private async Task DeleteExecutionAsync()
    {
        if (!await Confirmer.ConfirmAsync("Delete execution?", "Deleting executions that might be running can lead to undefined behaviour of the executor service. Are you sure you want to delete this execution?"))
        {
            return;
        }
        try
        {
            await Mediator.SendAsync(new DeleteExecutionCommand(ExecutionId));
            NavigationManager.NavigateTo("/executions");
            Toaster.AddSuccess("Execution deleted successfully");
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error deleting execution", ex.Message);
        }
    }

    public void Dispose()
    {
        timer.Stop();
        timer.Dispose();
        cts.Cancel();
        cts.Dispose();
    }
}
