using Biflow.Core;
using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Components;
using Biflow.Ui.Core;
using Biflow.Ui.Core.Projection;
using Biflow.Ui.Shared.Executions;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Timers;

namespace Biflow.Ui.Pages;

[Route("/executions/{ExecutionId:guid}")]
public partial class ExecutionDetails : ComponentBase, IDisposable
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;
    
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    
    [Inject] private IExecutorService ExecutorService { get; set; } = null!;

    [Parameter] public Guid ExecutionId { get; set; }

    private DependencyGraph<StepExecution>? DependencyGraph { get; set; }

    private Execution? Execution { get; set; }

    private IEnumerable<StepExecutionAttempt>? Executions => Execution?.StepExecutions.SelectMany(e => e.StepExecutionAttempts);

    private IEnumerable<StepExecutionProjection>? FilteredExecutions => Executions
        ?.Where(e => TagFilter.Count == 0 || e.StepExecution.Step?.Tags.Any(t => TagFilter.Contains(t.TagName)) == true)
        .Where(e => StepStatusFilter.Count == 0 || StepStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => StepFilter.Count == 0 || StepFilter.Contains((e.StepExecution.StepName, e.StepExecution.StepType)))
        .Where(e => StepTypeFilter.Count == 0 || StepTypeFilter.Contains(e.StepExecution.StepType))
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

    private Report ShowReport { get; set; } = Report.Table;

    private enum Report { Table, Gantt, Dependencies, ExecutionDetails, Parameters, Rerun, History }

    private readonly System.Timers.Timer timer = new(5000) { AutoReset = false };

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

    private bool Loading { get; set; } = false;

    private bool Stopping => StoppingExecutions.Any(id => id == ExecutionId);

    // Maintain a list executions that are being stopped.
    // This same component instance can be used to switch between different job executions.
    // This list allows for stopping multiple executions concurrently
    // and to modify the view based on which job execution is being shown.
    private List<Guid> StoppingExecutions { get; set; } = [];

    private HashSet<StepExecutionStatus> StepStatusFilter { get; } = [];
    private HashSet<(string StepName, StepType StepType)> StepFilter { get; } = [];
    private StepExecution? DependencyGraphStepFilter { get; set; }
    private HashSet<StepType> StepTypeFilter { get; } = [];
    private HashSet<string> TagFilter { get; } = [];
    private SortMode SortMode { get; set; } = SortMode.StartedAsc;

    private StepExecutionDetailsOffcanvas? StepExecutionDetailsOffcanvas { get; set; }

    private StepHistoryOffcanvas? StepHistoryOffcanvas { get; set; }

    private bool GraphShouldRender { get; set; } = false;

    private int FilterDepthBackwards
    {
        get => _filterDepthBackwards;
        set => _filterDepthBackwards = value >= 0 ? value : _filterDepthBackwards;
    }

    private int _filterDepthBackwards;

    private int FilterDepthForwards
    {
        get => _filterDepthForwards;
        set => _filterDepthForwards = value >= 0 ? value : _filterDepthForwards;
    }

    private int _filterDepthForwards;

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
        Report.Table or Report.Gantt or Report.Dependencies or Report.ExecutionDetails or Report.Parameters => true,
        _ => false
    };

    protected override async Task OnParametersSetAsync()
    {
        // If the ExecutionId parameter is updated when either the rerun or history view is being shown,
        // go back to the table view and refresh (the execution displayed was changed).
        if (ShowReport == Report.Rerun || ShowReport == Report.History)
        {
            ShowReport = Report.Table;
            StateHasChanged();
        }
        await LoadData();
    }

    private async Task LoadData()
    {
        if (ExecutionId != Guid.Empty)
        {
            timer.Stop();
            Loading = true;
            await InvokeAsync(StateHasChanged);
            using var context = DbFactory.CreateDbContext();
            Execution = await context.Executions
                .AsNoTrackingWithIdentityResolution()
                .Include(e => e.Job)
                .Include(e => e.Schedule)
                .Include(e => e.ExecutionParameters)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.StepExecutionAttempts)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.ExecutionDependencies)
                .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
                .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.ExecutionConditionParameters)
                .ThenInclude(p => p.ExecutionParameter)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.Step)
                .ThenInclude(s => s!.Tags)
                .FirstOrDefaultAsync(e => e.ExecutionId == ExecutionId);
            Loading = false;
            if (AutoRefresh && (Execution?.ExecutionStatus == ExecutionStatus.Running || Execution?.ExecutionStatus == ExecutionStatus.NotStarted))
            {
                timer.Start();
            }
            else
            {
                AutoRefresh = false;
            }
            await InvokeAsync(StateHasChanged);
            GraphShouldRender = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (GraphShouldRender && ShowReport == Report.Dependencies)
        {
            await LoadGraph();
        }
    }

    private async Task LoadGraph()
    {
        ArgumentNullException.ThrowIfNull(DependencyGraph);
        ArgumentNullException.ThrowIfNull(Execution);
        GraphShouldRender = false;

        DependencyGraphNode[] nodes;
        DependencyGraphEdge[] edges;
        if (DependencyGraphStepFilter is null)
        {
            // Create a list of steps and dependencies and send them through JSInterop as JSON objects.
            nodes = Execution.StepExecutions
                .Select(step =>
                {
                    var status = step.ExecutionStatus.ToString() ?? "";
                    return new DependencyGraphNode(
                        Id: step.StepId.ToString(),
                        Name: step.StepName,
                        CssClass: $"enabled {status.ToLower()} internal",
                        TooltipText: $"{step.StepType}, {status}, {step.GetDurationInSeconds().SecondsToReadableFormat()}",
                        EnableOnClick: true
                    );
                }).ToArray();
            edges = Execution.StepExecutions
                .SelectMany(step => step.ExecutionDependencies)
                .Where(dep => dep.DependantOnStepExecution is not null)
                .Select(dep => new DependencyGraphEdge(
                    Id: dep.StepId.ToString(),
                    DependsOnId: dep.DependantOnStepId.ToString(),
                    CssClass: dep.DependencyType.ToString().ToLower()
                )).ToArray();
        }
        else
        {
            var startStep = Execution?.StepExecutions.FirstOrDefault(s => s.StepId == DependencyGraphStepFilter.StepId);
            if (startStep is not null)
            {
                var steps = RecurseDependenciesBackward(startStep, [], 0);
                steps.Remove(startStep);
                steps = RecurseDependenciesForward(startStep, steps, 0);

                nodes = steps.Select(step => new DependencyGraphNode(
                    Id: step.StepId.ToString(),
                    Name: step.StepName,
                    CssClass: $"enabled {step.ExecutionStatus?.ToString().ToLower() ?? ""} internal",
                    TooltipText: $"{step.StepType}",
                    EnableOnClick: true
                )).ToArray();
                edges = steps
                    .SelectMany(step => step.ExecutionDependencies)
                    .Where(d => steps.Any(s => d.DependantOnStepId == s.StepId) && steps.Any(s => d.StepId == s.StepId)) // only include dependencies whose step is included
                    .Select(dep => new DependencyGraphEdge(
                        Id: dep.StepId.ToString(),
                        DependsOnId: dep.DependantOnStepId.ToString(),
                        CssClass: dep.DependencyType.ToString().ToLower()
                    )).ToArray();                
            }
            else
            {
                return;
            }
        }
        await DependencyGraph.DrawAsync(nodes, edges);
        StateHasChanged();
    }

    private async Task ShowStepExecutionOffcanvas(StepExecution step)
    {
        var attempt = step?.StepExecutionAttempts.OrderByDescending(s => s.StartDateTime).First();
        if (attempt is null)
        {
            return;
        }
        StateHasChanged();
        await StepExecutionDetailsOffcanvas.LetAsync(x => x.ShowAsync(attempt));
    }

    private async Task StopJobExecutionAsync()
    {
        if (Stopping)
        {
            Messenger.AddInformation("Execution is already stopping");
            return;
        }

        if (Execution is null)
        {
            Messenger.AddError("Execution was null");
            return;
        }

        StoppingExecutions.Add(ExecutionId);
        try
        {
            string username = HttpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? throw new ArgumentNullException(nameof(username), "Username cannot be null");
            await ExecutorService.StopExecutionAsync(Execution.ExecutionId, username);
            Messenger.AddInformation("Stop request sent successfully to the executor service");
        }
        catch (TimeoutException)
        {
            Messenger.AddError("Operation timed out", "The executor process may no longer be running");
            StoppingExecutions.RemoveAll(id => id == ExecutionId);
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error stopping execution", ex.Message);
            StoppingExecutions.RemoveAll(id => id == ExecutionId);
        }
    }

    private List<StepExecution> RecurseDependenciesBackward(StepExecution step, List<StepExecution> processedSteps, int depth)
    {
        ArgumentNullException.ThrowIfNull(Execution?.StepExecutions);

        // If the step was already handled, return.
        // This way we do not loop indefinitely in case of circular dependencies.
        if (processedSteps.Any(s => s.StepId == step.StepId))
        {
            return processedSteps;
        }

        if (depth++ > FilterDepthBackwards && FilterDepthBackwards > 0)
        {
            depth--;
            return processedSteps;
        }

        processedSteps.Add(step);

        // Get dependency steps.
        List<StepExecution> dependencySteps = Execution.StepExecutions.Where(s => step.ExecutionDependencies.Any(d => s.StepId == d.DependantOnStepId)).ToList();

        // Loop through the dependencies and handle them recursively.
        foreach (var depencyStep in dependencySteps)
        {
            RecurseDependenciesBackward(depencyStep, processedSteps, depth);
        }

        depth--;

        return processedSteps;
    }

    private List<StepExecution> RecurseDependenciesForward(StepExecution step, List<StepExecution> processedSteps, int depth)
    {
        ArgumentNullException.ThrowIfNull(Execution?.StepExecutions);
        if (processedSteps.Any(s => s.StepId == step.StepId))
        {
            return processedSteps;
        }

        if (depth++ > FilterDepthForwards && FilterDepthForwards > 0)
        {
            depth--;
            return processedSteps;
        }

        processedSteps.Add(step);

        List<StepExecution> dependencySteps = Execution.StepExecutions.Where(s => s.ExecutionDependencies.Any(d => d.DependantOnStepId == step.StepId)).ToList();

        foreach (var depencyStep in dependencySteps)
        {
            RecurseDependenciesForward(depencyStep, processedSteps, depth);
        }

        depth--;

        return processedSteps;
    }

    private Task<AutosuggestDataProviderResult<StepExecution>> ProvideSuggestions(AutosuggestDataProviderRequest request)
    {
        ArgumentNullException.ThrowIfNull(Execution);
        var filtered = Execution.StepExecutions.Where(s => s.StepName?.ContainsIgnoreCase(request.UserInput) ?? false);
        return Task.FromResult(new AutosuggestDataProviderResult<StepExecution>
        {
            Data = filtered
        });
    }

    private static string TextSelector(StepExecution step) => step.StepName ?? "";

    public void Dispose()
    {
        timer.Stop();
        timer.Dispose();
    }
}
