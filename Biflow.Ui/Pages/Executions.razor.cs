using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Biflow.Ui.Pages;

[Route("/executions")]
public partial class Executions : ComponentBase, IAsyncDisposable
{
    [Inject] private IDbContextFactory<BiflowContext> DbContextFactory { get; set; } = null!;
        
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Inject] private IHxMessengerService Messenger { get; set; } = null!;

    private bool SessionStorageRetrieved { get; set; } = false;

    private bool ShowSteps { get; set; } = false;
    
    private bool ShowGraph { get; set; } = false;
    
    private bool Loading { get; set; } = false;

    private DateTime FromDateTime
    {
        get => _fromDateTime;
        set => _fromDateTime = value > ToDateTime ? ToDateTime : value;
    }
    private DateTime _fromDateTime = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddHours(-1);

    private DateTime ToDateTime
    {
        get => _toDateTime;
        set => _toDateTime = value < FromDateTime ? FromDateTime : value;
    }
    private DateTime _toDateTime = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddMinutes(1);

    private List<Execution>? Executions_ { get; set; }

    private HashSet<ExecutionStatus> JobStatusFilter { get; set; } = new();
    
    private HashSet<StepExecutionStatus> StepStatusFilter { get; set; } = new();
    
    private HashSet<string> JobFilter { get; set; } = new();
    
    private HashSet<string> StepFilter { get; set; } = new();
    
    private HashSet<StepType> StepTypeFilter { get; set; } = new();
    
    private HashSet<string> TagFilter { get; set; } = new();
    
    private StartType StartTypeFilter { get; set; } = StartType.All;

    private IEnumerable<Execution>? FilteredExecutions => Executions_?
                        .Where(e => !JobStatusFilter.Any() || JobStatusFilter.Contains(e.ExecutionStatus))
                        .Where(e => !JobFilter.Any() || JobFilter.Contains(e.JobName))
                        .Where(e => StartTypeFilter == StartType.All ||
                        StartTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
                        StartTypeFilter == StartType.Manual && e.ScheduleId is null);

    private IEnumerable<StepExecutionAttempt>? FilteredStepExecutions => Executions_?
                        .Where(e => StartTypeFilter == StartType.All ||
                        StartTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
                        StartTypeFilter == StartType.Manual && e.ScheduleId is null)
                        .SelectMany(e => e.StepExecutions)
                        .Where(e => !TagFilter.Any() || e.Step?.Tags.Any(t => TagFilter.Contains(t.TagName)) == true)
                        .SelectMany(s => s.StepExecutionAttempts)
                        .Where(e => !StepStatusFilter.Any() || StepStatusFilter.Contains(e.ExecutionStatus))
                        .Where(e => !JobFilter.Any() || JobFilter.Contains(e.StepExecution.Execution.JobName))
                        .Where(e => !StepFilter.Any() || StepFilter.Contains(e.StepExecution.StepName))
                        .Where(e => !StepTypeFilter.Any() || StepTypeFilter.Contains(e.StepExecution.StepType))
                        .OrderByDescending(e => e.StepExecution.Execution.CreatedDateTime)
                        .ThenByDescending(e => e.StartDateTime)
                        .ThenByDescending(e => e.StepExecution.ExecutionPhase);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await GetSessionStorageValues();
            }
            catch (Exception ex)
            {
                Messenger.AddWarning("Error getting session storage values", ex.Message);
            }

            SessionStorageRetrieved = true;
            StateHasChanged();
            await LoadData();
        }
    }

    private async Task LoadData()
    {
        Loading = true;
        StateHasChanged();
        using var context = await Task.Run<BiflowContext>(DbContextFactory.CreateDbContext);

        var query = context.Executions
            // Index optimized way of querying executions without having to scan the entire table.
            .Where(e => e.CreatedDateTime <= ToDateTime && e.EndDateTime >= FromDateTime)
            .Union(context.Executions
                // Adds executions that were not started.
                .Where(e => e.CreatedDateTime >= FromDateTime && e.CreatedDateTime <= ToDateTime && e.EndDateTime == null))
            .Union(context.Executions
                // Adds currently running executions if current time fits in the time window.
                .Where(e => DateTime.Now >= FromDateTime && DateTime.Now <= ToDateTime && e.ExecutionStatus == ExecutionStatus.Running));

        Executions_ = await query
            .AsNoTrackingWithIdentityResolution()
            .Include(e => e.ExecutionParameters)
            .Include(e => e.StepExecutions)
            .ThenInclude(exec => exec.StepExecutionAttempts)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => (e as ParameterizedStepExecution)!.StepExecutionParameters)
            .ThenInclude(p => p.ExecutionParameter)
            .Include(e => e.StepExecutions)
            .ThenInclude(e => e.ExecutionConditionParameters)
            .ThenInclude(p => p.ExecutionParameter)
            .Include(execution => execution.StepExecutions)
            .ThenInclude(exec => exec.Step)
            .ThenInclude(s => s!.Tags)
            .OrderByDescending(execution => execution.CreatedDateTime)
            .ThenByDescending(execution => execution.StartDateTime)
            .ToListAsync();
        Loading = false;
        StateHasChanged();
    }

    private async Task SelectPresetLast(int hours)
    {
        ToDateTime = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddMinutes(1);
        FromDateTime = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddHours(-hours);
        await LoadData();
    }

    private async Task SelectPreset(DateTime from, DateTime to)
    {
        FromDateTime = from.Trim(TimeSpan.TicksPerMinute);
        ToDateTime = to.Trim(TimeSpan.TicksPerMinute);
        await LoadData();
    }

    private record SessionStorage(
        DateTime FromDateTime,
        DateTime ToDateTime,
        bool ShowSteps,
        bool ShowGraph,
        StartType StartType,
        HashSet<ExecutionStatus> JobStatuses,
        HashSet<StepExecutionStatus> StepStatuses,
        HashSet<string> JobNames,
        HashSet<string> StepNames,
        HashSet<StepType> StepTypes,
        HashSet<string> Tags
    );

    private async Task SetSessionStorageValues()
    {
        var sessionStorage = new SessionStorage(
            FromDateTime,
            ToDateTime,
            ShowSteps,
            ShowGraph,
            StartTypeFilter,
            JobStatusFilter,
            StepStatusFilter,
            JobFilter,
            StepFilter,
            StepTypeFilter,
            TagFilter
        );
        var text = JsonSerializer.Serialize(sessionStorage);
        await JS.InvokeVoidAsync("sessionStorage.setItem", "ExecutionsSessionStorage", text);
    }

    private async Task GetSessionStorageValues()
    {
        var text = await JS.InvokeAsync<string>("sessionStorage.getItem", "ExecutionsSessionStorage");
        if (text is null) return;
        var sessionStorage = JsonSerializer.Deserialize<SessionStorage>(text);
        FromDateTime = sessionStorage?.FromDateTime ?? FromDateTime;
        ToDateTime = sessionStorage?.ToDateTime ?? ToDateTime;
        ShowSteps = sessionStorage?.ShowSteps ?? ShowSteps;
        ShowGraph = sessionStorage?.ShowGraph ?? ShowGraph;
        StartTypeFilter = sessionStorage?.StartType ?? StartTypeFilter;
        JobStatusFilter = sessionStorage?.JobStatuses ?? JobStatusFilter;
        StepStatusFilter = sessionStorage?.StepStatuses ?? StepStatusFilter;
        JobFilter = sessionStorage?.JobNames ?? JobFilter;
        StepFilter = sessionStorage?.StepNames ?? StepFilter;
        StepTypeFilter = sessionStorage?.StepTypes ?? StepTypeFilter;
        TagFilter = sessionStorage?.Tags ?? TagFilter;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await SetSessionStorageValues();
        }
        catch (Exception ex)
        {
            Messenger.AddWarning("Error saving session storage values", ex.Message);
        }
    }
}
