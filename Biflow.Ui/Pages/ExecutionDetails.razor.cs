using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.Executions;
using Biflow.Utilities;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Biflow.Ui.Pages;

[Route("/executions/{ExecutionId:guid}")]
public partial class ExecutionDetails : ComponentBase, IAsyncDisposable
{
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private IExecutorService ExecutorService { get; set; } = null!;

    [Parameter] public Guid ExecutionId { get; set; }

    private Execution? Execution { get; set; }

    private IEnumerable<StepExecutionAttempt> Executions =>
        Execution?.StepExecutions
        .SelectMany(e => e.StepExecutionAttempts)
        ?? Enumerable.Empty<StepExecutionAttempt>();

    private IEnumerable<StepExecutionSlim> FilteredExecutions => Executions
        .Where(e => !TagFilter.Any() || e.StepExecution.Step?.Tags.Any(t => TagFilter.Contains(t.TagName)) == true)
        .Where(e => !StepStatusFilter.Any() || StepStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => !StepFilter.Any() || StepFilter.Contains((e.StepExecution.StepName, e.StepExecution.StepType)))
        .Where(e => !StepTypeFilter.Any() || StepTypeFilter.Contains(e.StepExecution.StepType))
        .Select(e => new StepExecutionSlim(
            e.StepExecution.ExecutionId,
            e.StepExecution.StepId,
            e.RetryAttemptIndex,
            e.StepExecution.Step!.StepName ?? e.StepExecution.StepName,
            e.StepType,
            e.StepExecution.ExecutionPhase,
            e.StartDateTime,
            e.EndDateTime,
            e.ExecutionStatus,
            e.StepExecution.Execution.ExecutionStatus,
            e.StepExecution.Execution.DependencyMode,
            e.StepExecution.Execution.ScheduleId,
            e.StepExecution.Execution.JobId,
            e.StepExecution.Execution.Job!.JobName ?? e.StepExecution.Execution.JobName,
            e.StepExecution.Step.Tags));

    private Report ShowReport { get; set; } = Report.Table;

    private enum Report { Table, Gantt, Dependencies, Rerun, History }

    private bool Loading { get; set; } = false;

    private bool JobExecutionDetailsOpen { get; set; } = false;

    private bool ParametersOpen { get; set; } = false;

    private bool Stopping => StoppingExecutions.Any(id => id == ExecutionId);

    // Maintain a list executions that are being stopped.
    // This same component instance can be used to switch between different job executions.
    // This list allows for stopping multiple executions concurrently
    // and to modify the view based on which job execution is being shown.
    private List<Guid> StoppingExecutions { get; set; } = new();

    private HashSet<StepExecutionStatus> StepStatusFilter { get; } = new();
    private HashSet<(string StepName, StepType StepType)> StepFilter { get; } = new();
    private StepExecution? DependencyGraphStepFilter { get; set; }
    private HashSet<StepType> StepTypeFilter { get; } = new();
    private HashSet<string> TagFilter { get; } = new();
    private SortMode SortMode { get; set; } = SortMode.StartedAsc;

    private StepExecutionDetailsOffcanvas? StepExecutionDetailsOffcanvas { get; set; }
    private StepExecutionAttempt? SelectedStepExecutionAttempt { get; set; }

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

    protected override async Task OnParametersSetAsync()
    {
        if (ShowReport == Report.Rerun)
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
            Loading = true;
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
            StateHasChanged();
            Loading = false;

            GraphShouldRender = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (GraphShouldRender && ShowReport == Report.Dependencies)
        {
            await LoadGraph();
        }
        if (firstRender)
        {
            await JS.InvokeVoidAsync("attachDependencyGraphBodyListener");
        }
    }

    private async Task LoadGraph()
    {
        GraphShouldRender = false;

        string? stepsJson = null;
        string? dependenciesJson = null;

        if (DependencyGraphStepFilter is null)
        {
            // Create a list of steps and dependencies and send them through JSInterop as JSON objects.
            var steps = Execution?.StepExecutions
                .Select(step =>
                {
                    var status = step.ExecutionStatus.ToString() ?? "";
                    return new
                    {
                        Id = step.StepId,
                        Name = step.StepName,
                        ClassName = $"enabled {status.ToLower()}",
                        Tooltip = $"{step.StepType}, {status}, {step.GetDurationInSeconds().SecondsToReadableFormat()}"
                    };
                });
            var dependencies = Execution?.StepExecutions
                .SelectMany(step => step.ExecutionDependencies)
                .Where(dep => dep.DependantOnStepExecution is not null)
                .Select(dep => new
                {
                    dep.StepId,
                    dep.DependantOnStepId,
                    ClassName = dep.DependencyType.ToString().ToLower()
                });

            stepsJson = JsonSerializer.Serialize(steps);
            dependenciesJson = JsonSerializer.Serialize(dependencies);
        }
        else
        {
            var startStep = Execution?.StepExecutions.FirstOrDefault(s => s.StepId == DependencyGraphStepFilter.StepId);
            if (startStep is not null)
            {
                var steps = RecurseDependenciesBackward(startStep, new(), 0);
                steps.Remove(startStep);
                steps = RecurseDependenciesForward(startStep, steps, 0);

                var dependencies = steps
                    .SelectMany(step => step.ExecutionDependencies)
                    .Where(d => steps.Any(s => d.DependantOnStepId == s.StepId) && steps.Any(s => d.StepId == s.StepId)) // only include dependencies whose step is included
                    .Select(dep => new
                    {
                        dep.StepId,
                        dep.DependantOnStepId,
                        ClassName = dep.DependencyType.ToString().ToLower()
                    });

                stepsJson = JsonSerializer.Serialize(steps.Select(step => new
                {
                    Id = step.StepId,
                    Name = step.StepName,
                    ClassName = $"enabled {step.ExecutionStatus?.ToString().ToLower() ?? ""}",
                    Tooltip = $"{step.StepType}"
                }));
                dependenciesJson = JsonSerializer.Serialize(dependencies);
            }
        }
        

        if (stepsJson is not null && dependenciesJson is not null)
            await JS.InvokeVoidAsync("drawDependencyGraph", stepsJson, dependenciesJson);

        StateHasChanged();
    }

    private async Task ShowStepExecutionOffcanvas(StepExecution step)
    {
        var attempt = step?.StepExecutionAttempts.OrderByDescending(s => s.StartDateTime).First();
        SelectedStepExecutionAttempt = attempt;
        StateHasChanged();
        await StepExecutionDetailsOffcanvas.LetAsync(x => x.ShowAsync());
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

    private async Task<AutosuggestDataProviderResult<StepExecution>> ProvideSuggestions(AutosuggestDataProviderRequest request)
    {
        ArgumentNullException.ThrowIfNull(Execution);
        await Task.Delay(150);
        var filtered = Execution.StepExecutions.Where(s => s.StepName?.ContainsIgnoreCase(request.UserInput) ?? false);
        return new AutosuggestDataProviderResult<StepExecution>
        {
            Data = filtered
        };
    }

    private string TextSelector(StepExecution step) => step.StepName ?? "";

    public async ValueTask DisposeAsync()
    {
        await JS.InvokeVoidAsync("disposeDependencyGraphBodyListener");
    }
}
