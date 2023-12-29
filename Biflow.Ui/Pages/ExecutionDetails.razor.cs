using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Core.Projection;
using Biflow.Ui.Shared.Executions;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Timers;

namespace Biflow.Ui.Pages;

[Route("/executions/{ExecutionId:guid}/{Page}/{InitialStepId:guid?}")]
public partial class ExecutionDetails : ComponentBase, IDisposable
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private IExecutorService ExecutorService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IHxMessageBoxService Confirmer { get; set; } = null!;

    [Parameter] public Guid ExecutionId { get; set; }

    [Parameter] public string Page { get; set; } = "list";

    [Parameter] public Guid? InitialStepId { get; set; }

    private readonly System.Timers.Timer timer = new(5000) { AutoReset = false };
    private readonly HashSet<StepType> stepTypeFilter = [];
    private readonly HashSet<string> tagFilter = [];
    private readonly HashSet<StepExecutionStatus> stepStatusFilter = [];
    private readonly HashSet<(string StepName, StepType StepType)> stepFilter = [];
    private Guid prevExecutionId;
    private Execution? execution;
    private Job? job;
    private Schedule? schedule;
    private bool loading = false;
    private SortMode sortMode = SortMode.StartedAsc;
    private ExecutionParameterLineageOffcanvas? parameterLineageOffcanvas;

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

    private IEnumerable<StepExecutionAttempt>? Executions => execution?.StepExecutions.SelectMany(e => e.StepExecutionAttempts);

    private IEnumerable<StepExecutionProjection>? FilteredExecutions => Executions
        ?.Where(e => tagFilter.Count == 0 || e.StepExecution.Step?.Tags.Any(t => tagFilter.Contains(t.TagName)) == true)
        .Where(e => stepStatusFilter.Count == 0 || stepStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => stepFilter.Count == 0 || stepFilter.Contains((e.StepExecution.StepName, e.StepExecution.StepType)))
        .Where(e => stepTypeFilter.Count == 0 || stepTypeFilter.Contains(e.StepExecution.StepType))
        .Select(e => new StepExecutionProjection(
            e.StepExecution.ExecutionId,
            e.StepExecution.StepId,
            e.RetryAttemptIndex,
            e.StepExecution.Step?.StepName ?? e.StepExecution.StepName,
            e.StepType,
            e.StepExecution.ExecutionPhase,
            e.StartedOn,
            e.EndedOn,
            e.ExecutionStatus,
            e.StepExecution.Execution.ExecutionStatus,
            e.StepExecution.Execution.DependencyMode,
            e.StepExecution.Execution.ScheduleId,
            e.StepExecution.Execution.JobId,
            job?.JobName ?? e.StepExecution.Execution.JobName,
            e.StepExecution.Step?.Tags.ToArray() ?? []));

    private Report ShowReport => Page switch
    {
        "gantt" => Report.Gantt,
        "graph" => Report.Graph,
        "executiondetails" => Report.ExecutionDetails,
        "parameters" => Report.Parameters,
        "rerun" => Report.Rerun,
        "history" => Report.History,
        "statuses" => Report.Statuses,
        _ => Report.List
    };

    private enum Report { List, Gantt, Graph, ExecutionDetails, Parameters, Rerun, History, Statuses }

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
            execution = await context.Executions
                .AsNoTrackingWithIdentityResolution()
                .Include(e => e.ExecutionParameters)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.StepExecutionAttempts)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.ExecutionDependencies)
                .Include($"{nameof(execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
                .Include($"{nameof(execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.ExecutionConditionParameters)
                .ThenInclude(p => p.ExecutionParameter)
                .Include(e => e.StepExecutions)
                .FirstOrDefaultAsync(e => e.ExecutionId == ExecutionId);
            if (execution is not null)
            {
                var stepIds = execution.StepExecutions.Select(s => s.StepId).ToArray();
                var steps = await context.Steps
                    .AsNoTrackingWithIdentityResolution()
                    .Include(s => s.Tags)
                    .Where(s => stepIds.Contains(s.StepId))
                    .ToArrayAsync();
                var matches = steps.Join(execution.StepExecutions, s => s.StepId, e => e.StepId, (s, e) => (s, e));
                foreach (var (step, stepExecution) in matches)
                {
                    stepExecution.Step = step;
                }
            }
            job = execution is not null
                ? await context.Jobs.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(j => j.JobId == execution.JobId)
                : null;
            schedule = execution?.ScheduleId is not null
                ? await context.Schedules.AsNoTrackingWithIdentityResolution().FirstOrDefaultAsync(s => s.ScheduleId == execution.ScheduleId)
                : null;
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
        }
    }

    private async Task StopJobExecutionAsync()
    {
        if (Stopping)
        {
            Messenger.AddInformation("Execution is already stopping");
            return;
        }

        if (execution is null)
        {
            Messenger.AddError("Execution was null");
            return;
        }

        stoppingExecutions.Add(ExecutionId);
        try
        {
            string username = HttpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? throw new ArgumentNullException(nameof(username), "Username cannot be null");
            await ExecutorService.StopExecutionAsync(execution.ExecutionId, username);
            Messenger.AddInformation("Stop request sent successfully to the executor service");
        }
        catch (TimeoutException)
        {
            Messenger.AddError("Operation timed out", "The executor process may no longer be running");
            stoppingExecutions.RemoveAll(id => id == ExecutionId);
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error stopping execution", ex.Message);
            stoppingExecutions.RemoveAll(id => id == ExecutionId);
        }
    }

    private async Task UpdateExecutionStatusAsync(ExecutionStatus status)
    {
        try
        {
            using var context = await DbFactory.CreateDbContextAsync();
            await context.Executions
                .Where(e => e.ExecutionId == ExecutionId)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(e => e.ExecutionStatus, status)
                    .SetProperty(e => e.StartedOn, e => e.StartedOn ?? DateTimeOffset.Now)
                    .SetProperty(e => e.EndedOn, e => e.EndedOn ?? DateTimeOffset.Now));
            if (execution is not null)
            {
                execution.ExecutionStatus = status;
                execution.StartedOn ??= DateTimeOffset.Now;
                execution.EndedOn ??= DateTimeOffset.Now;
            }
            Messenger.AddInformation("Status updated successfully");
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error updating status", ex.Message);
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
            using var context = await DbFactory.CreateDbContextAsync();
            var execution = await context.Executions
                .Include(e => e.ExecutionParameters)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.ExecutionDependencies)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.DependantExecutions)
                .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                .FirstOrDefaultAsync(e => e.ExecutionId == ExecutionId);
            if (execution is not null)
            {
                context.Executions.Remove(execution);
                await context.SaveChangesAsync();
            }
            NavigationManager.NavigateTo("/executions");
            Messenger.AddInformation("Execution deleted successfully");
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error deleting execution", ex.Message);
        }
    }

    public void Dispose()
    {
        timer.Stop();
        timer.Dispose();
    }
}
