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
            e.StartDateTime,
            e.EndDateTime,
            e.ExecutionStatus,
            e.StepExecution.Execution.ExecutionStatus,
            e.StepExecution.Execution.DependencyMode,
            e.StepExecution.Execution.ScheduleId,
            e.StepExecution.Execution.JobId,
            e.StepExecution.Execution.Job?.JobName ?? e.StepExecution.Execution.JobName,
            e.StepExecution.Step?.Tags.ToArray() ?? []));

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
            execution = await context.Executions
                .AsNoTrackingWithIdentityResolution()
                .Include(e => e.Job)
                .Include(e => e.Schedule)
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
                .ThenInclude(e => e.Step)
                .ThenInclude(s => s!.Tags)
                .FirstOrDefaultAsync(e => e.ExecutionId == ExecutionId);
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

    public void Dispose()
    {
        timer.Stop();
        timer.Dispose();
    }
}
