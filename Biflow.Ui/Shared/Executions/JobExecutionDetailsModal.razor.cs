using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Utilities;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Biflow.Ui.Shared.Executions;

public partial class JobExecutionDetailsModal : ComponentBase, IDisposable
{
    [Inject] private MarkupHelperService MarkupHelper { get; set; } = null!;
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = null!;
    [Inject] private IExecutorService ExecutorService { get; set; } = null!;

    [Parameter] public string? ExecutionId_ { get; set; }

    private Guid ExecutionId => ExecutionId_ switch { not null => Guid.Parse(ExecutionId_), _ => Guid.Empty };

    private HxModal? Modal { get; set; }

    private Execution? Execution { get; set; }

    private IEnumerable<StepExecutionAttempt> Executions =>
        Execution?.StepExecutions
        .SelectMany(e => e.StepExecutionAttempts)
        ?? Enumerable.Empty<StepExecutionAttempt>();

    private IEnumerable<StepExecutionAttempt> FilteredExecutions => Executions
        .Where(e => !TagFilter.Any() || e.StepExecution.Step?.Tags.Any(t => TagFilter.Contains(t.TagName)) == true)
        .Where(e => !StepStatusFilter.Any() || StepStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => !StepFilter.Any() || StepFilter.Contains(e.StepExecution.StepName))
        .Where(e => !StepTypeFilter.Any() || StepTypeFilter.Contains(e.StepExecution.StepType));

    private Report ShowReport { get; set; } = Report.Table;

    private enum Report { Table, Gantt, Dependencies, Rerun }

    private bool Loading { get; set; } = false;

    private bool JobExecutionDetailsOpen { get; set; } = false;

    private bool Stopping => StoppingExecutions.Any(id => id == ExecutionId);

    // Maintain a list executions that are being stopped.
    // This same component instance can be used to switch between different job executions.
    // This list allows for stopping multiple executions concurrently
    // and to modify the view based on which job execution is being shown.
    private List<Guid> StoppingExecutions { get; set; } = new();

    private HashSet<StepExecutionStatus> StepStatusFilter { get; } = new();
    private HashSet<string> StepFilter { get; } = new();
    private HashSet<StepType> StepTypeFilter { get; } = new();
    private HashSet<string> TagFilter { get; } = new();
    private SortMode SortMode_ { get; set; } = SortMode.StartedAsc;
    private enum SortMode { StartedAsc, StartedDesc, DurationAsc, DurationDesc }

    private StepExecutionDetailsOffcanvas? StepExecutionDetailsOffcanvas { get; set; }
    private StepExecutionAttempt? SelectedStepExecutionAttempt { get; set; }

    private DotNetObjectReference<MethodInvokeHelper> ObjectReference { get; set; } = null!;

    private bool GraphShouldRender { get; set; } = false;

    protected override void OnInitialized()
    {
        // Create a DotNetObjectReference with a new helper method tied to an instance of this component.
        // This will allow JS to call back to a specific instance of this component.
        // This needs to be done, because multiple users might be using this component concurrently.
        var helper = new MethodInvokeHelper(ShowStepExecutionOffcanvas);
        ObjectReference = DotNetObjectReference.Create(helper);
    }

    protected override async Task OnParametersSetAsync()
    {
        Execution = null;
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
                .Include(e => e.ExecutionParameters)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.StepExecutionAttempts)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.ExecutionDependencies)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => (e as ParameterizedStepExecution)!.StepExecutionParameters)
                .ThenInclude(p => p.ExecutionParameter)
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
            await LoadGraph();
    }

    private async Task LoadGraph()
    {
        GraphShouldRender = false;

        // Create a list of steps and dependencies and send them through JSInterop as JSON objects.
        var steps = Execution?.StepExecutions
            .Select(step =>
            {
                var status = step.GetExecutionStatus().ToString().ToLower();
                return new
                {
                    Id = step.StepId,
                    Name = step.StepName,
                    ClassName = $"enabled {status}",
                    Tooltip = $"{step.StepType}, {step.GetExecutionStatus()}, {step.GetDurationInSeconds().SecondsToReadableFormat()}"
                };
            });
        var dependencies = Execution?.StepExecutions
            .SelectMany(step => step.ExecutionDependencies)
            .Select(dep => new
            {
                dep.StepId,
                dep.DependantOnStepId,
                ClassName = dep.DependencyType.ToString().ToLower()
            });

        var stepsJson = JsonSerializer.Serialize(steps);
        var dependenciesJson = JsonSerializer.Serialize(dependencies);

        if (stepsJson is not null && dependenciesJson is not null)
            await JS.InvokeVoidAsync("drawDependencyGraph", stepsJson, dependenciesJson, ObjectReference);

        StateHasChanged();
    }

    private async void ShowStepExecutionOffcanvas(string text)
    {
        if (Guid.TryParse(text, out Guid id))
        {
            var step = Execution?.StepExecutions.First(s => s.StepId == id);
            var attempt = step?.StepExecutionAttempts.OrderByDescending(s => s.StartDateTime).First();
            SelectedStepExecutionAttempt = attempt;
            StateHasChanged();
            await StepExecutionDetailsOffcanvas.LetAsync(x => x.ShowAsync());
        }
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
            await ExecutorService.StopExecutionAsync(Execution, username);
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

    private void OnClosed()
    {
        if (ShowReport == Report.Rerun)
        {
            ShowReport = Report.Table;
        }
    }

    public async Task ShowAsync() => await Modal.LetAsync(x => x.ShowAsync());

    public void Dispose() => ObjectReference?.Dispose();
}
