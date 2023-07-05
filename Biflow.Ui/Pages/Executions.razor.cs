using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Core.Projection;
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

    private Preset? ActivePreset { get; set; } = Preset.OneHour;

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

    private List<ExecutionProjection>? Executions_ { get; set; }

    private List<StepExecutionProjection>? StepExecutions { get; set; }

    private HashSet<ExecutionStatus> JobStatusFilter { get; set; } = new();
    
    private HashSet<StepExecutionStatus> StepStatusFilter { get; set; } = new();
    
    private HashSet<string> JobFilter { get; set; } = new();
    
    private HashSet<(string StepName, StepType StepType)> StepFilter { get; set; } = new();
    
    private HashSet<StepType> StepTypeFilter { get; set; } = new();
    
    private HashSet<string> TagFilter { get; set; } = new();
    
    private StartType StartTypeFilter { get; set; } = StartType.All;

    private IEnumerable<ExecutionProjection>? FilteredExecutions => Executions_?
        .Where(e => !JobStatusFilter.Any() || JobStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => !JobFilter.Any() || JobFilter.Contains(e.JobName))
        .Where(e => StartTypeFilter == StartType.All ||
        StartTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
        StartTypeFilter == StartType.Manual && e.ScheduleId is null);

    private IEnumerable<StepExecutionProjection>? FilteredStepExecutions => StepExecutions?
        .Where(e => StartTypeFilter == StartType.All ||
        StartTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
        StartTypeFilter == StartType.Manual && e.ScheduleId is null)
        .Where(e => !TagFilter.Any() || e.Tags.Any(t => TagFilter.Contains(t.TagName)) == true)
        .Where(e => !StepStatusFilter.Any() || StepStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => !JobFilter.Any() || JobFilter.Contains(e.JobName))
        .Where(e => !StepFilter.Any() || StepFilter.Contains((e.StepName, e.StepType)))
        .Where(e => !StepTypeFilter.Any() || StepTypeFilter.Contains(e.StepType));

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
            await LoadDataAsync();
        }
    }

    private async Task ShowExecutionsAsync()
    {
        Executions_ = null;
        ShowSteps = false;
        await LoadDataAsync();
    }

    private async Task ShowStepExecutionsAsync()
    {
        Executions_ = null;
        ShowSteps = true;
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        Loading = true;
        StateHasChanged();

        if (ActivePreset is Preset preset)
        {
            (FromDateTime, ToDateTime) = GetPreset(preset);
        }

        using var context = await Task.Run(DbContextFactory.CreateDbContext);

        if (!ShowSteps)
        {
            var query = context.Executions
                // Index optimized way of querying executions without having to scan the entire table.
                .Where(e => e.CreatedDateTime <= ToDateTime && e.EndDateTime >= FromDateTime);

            if (DateTime.Now >= FromDateTime && DateTime.Now <= ToDateTime)
            {
                query = query
                    .Concat(context.Executions
                        // Adds executions that were not started.
                        .Where(e => e.CreatedDateTime >= FromDateTime && e.CreatedDateTime <= ToDateTime && e.EndDateTime == null && e.ExecutionStatus != ExecutionStatus.Running))
                    .Concat(context.Executions
                        // Adds currently running executions if current time fits in the time window.
                        .Where(e => e.ExecutionStatus == ExecutionStatus.Running));
            }
            else
            {
                query = query
                    .Concat(context.Executions
                        // Adds executions that were not started and executions that may still be running.
                        .Where(e => e.CreatedDateTime >= FromDateTime && e.CreatedDateTime <= ToDateTime && e.EndDateTime == null));
            }
            Executions_ = await query
                .AsNoTracking()
                .AsSingleQuery()
                .OrderByDescending(e => e.CreatedDateTime)
                .ThenByDescending(e => e.StartDateTime)
                .Select(e => new ExecutionProjection(
                    e.ExecutionId,
                    e.JobId,
                    e.Job!.JobName ?? e.JobName,
                    e.ScheduleId,
                    e.CreatedDateTime,
                    e.StartDateTime,
                    e.EndDateTime,
                    e.ExecutionStatus,
                    e.StepExecutions.Count()))
                .ToListAsync();
        }
        else
        {
            var query = context.StepExecutionAttempts
                .Where(e => e.StepExecution.Execution.CreatedDateTime <= ToDateTime && e.StepExecution.Execution.EndDateTime >= FromDateTime);

            if (DateTime.Now >= FromDateTime && DateTime.Now <= ToDateTime)
            {
                query = query
                    // Adds executions that were not started.
                    .Union(context.StepExecutionAttempts
                        .Where(e => e.StepExecution.Execution.CreatedDateTime >= FromDateTime
                        && e.StepExecution.Execution.CreatedDateTime <= ToDateTime
                        && e.EndDateTime == null
                        && e.ExecutionStatus != StepExecutionStatus.Running))
                    // Adds currently running executions if current time fits in the time window.
                    .Union(context.StepExecutionAttempts
                        .Where(e => e.ExecutionStatus == StepExecutionStatus.Running));
            }
            else
            {
                query = query
                    // Adds executions that were not started and executions that may still be running.
                    .Union(context.StepExecutionAttempts
                        .Where(e => e.StepExecution.Execution.CreatedDateTime >= FromDateTime
                        && e.StepExecution.Execution.CreatedDateTime <= ToDateTime
                        && e.EndDateTime == null));
            }
            StepExecutions = await query
                .AsNoTracking()
                .OrderByDescending(e => e.StepExecution.Execution.CreatedDateTime)
                .ThenByDescending(e => e.StartDateTime)
                .ThenByDescending(e => e.StepExecution.ExecutionPhase)
                .Select(e => new StepExecutionProjection(
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
                    e.StepExecution.Step.Tags.ToList()))
                .ToListAsync();
        }

        Loading = false;
        StateHasChanged();
    }

    private async Task ApplyPresetAsync(Preset preset)
    {
        (FromDateTime, ToDateTime) = GetPreset(preset);
        ActivePreset = preset;
        await LoadDataAsync();
    }

    private (DateTime From, DateTime To) GetPreset(Preset preset)
    {
        var today = DateTime.Now.Date;
        var endToday = today.AddDays(1).AddTicks(-1);
        var startThisWeek = today.StartOfWeek(DayOfWeek.Monday);
        var endThisWeek = startThisWeek.AddDays(7).AddTicks(-1);
        var startThisMonth = new DateTime(today.Year, today.Month, 1);
        var endThisMonth = startThisMonth.AddMonths(1).AddTicks(-1);
        var yesterday = today.AddDays(-1);
        var endYesterday = today.AddTicks(-1);
        var startPrevWeek = today.AddDays(-7).StartOfWeek(DayOfWeek.Monday);
        var endPrevWeek = startPrevWeek.AddDays(7).AddTicks(-1);
        var prevMonth = today.AddMonths(-1);
        var startPrevMonth = new DateTime(prevMonth.Year, prevMonth.Month, 1);
        var endPrevMonth = startPrevMonth.AddMonths(1).AddTicks(-1);
        return preset switch
        {
            Preset.OneHour => GetPresetLast(1),
            Preset.ThreeHours => GetPresetLast(3),
            Preset.TwelveHours => GetPresetLast(12),
            Preset.TwentyFourHours => GetPresetLast(24),
            Preset.ThreeDays => GetPresetLast(72),
            Preset.SevenDays => GetPresetLast(168),
            Preset.FourteenDays => GetPresetLast(336),
            Preset.ThirtyDays => GetPresetLast(720),
            Preset.ThisDay => GetPresetBetween(today, endToday),
            Preset.ThisWeek => GetPresetBetween(startThisWeek, endThisWeek),
            Preset.ThisMonth => GetPresetBetween(startThisMonth, endThisMonth),
            Preset.PreviousDay => GetPresetBetween(yesterday, endYesterday),
            Preset.PreviousWeek => GetPresetBetween(startPrevWeek, endPrevWeek),
            Preset.PreviousMonth => GetPresetBetween(startPrevMonth, endPrevMonth),
            _ => (FromDateTime, ToDateTime)
        };
    }

    private static (DateTime From, DateTime To) GetPresetLast(int hours)
    {
        var to = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddMinutes(1);
        var from = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddHours(-hours);
        return (from, to);
    }

    private static (DateTime From, DateTime To) GetPresetBetween(DateTime from, DateTime to)
    {
        return (from.Trim(TimeSpan.TicksPerMinute), to.Trim(TimeSpan.TicksPerMinute));
    }

    private record SessionStorage(
        Preset? Preset,
        DateTime FromDateTime,
        DateTime ToDateTime,
        bool ShowSteps,
        bool ShowGraph,
        StartType StartType,
        HashSet<ExecutionStatus> JobStatuses,
        HashSet<StepExecutionStatus> StepStatuses,
        HashSet<string> JobNames,
        HashSet<(string StepName, StepType StepType)> StepNames,
        HashSet<StepType> StepTypes,
        HashSet<string> Tags
    );

    private async Task SetSessionStorageValues()
    {
        var sessionStorage = new SessionStorage(
            Preset: ActivePreset,
            FromDateTime: FromDateTime,
            ToDateTime: ToDateTime,
            ShowSteps: ShowSteps,
            ShowGraph: ShowGraph,
            StartType: StartTypeFilter,
            JobStatuses: JobStatusFilter,
            StepStatuses: StepStatusFilter,
            JobNames: JobFilter,
            StepNames: StepFilter,
            StepTypes: StepTypeFilter,
            Tags: TagFilter
        );
        var text = JsonSerializer.Serialize(sessionStorage, new JsonSerializerOptions { IncludeFields = true });
        await JS.InvokeVoidAsync("sessionStorage.setItem", "ExecutionsSessionStorage", text);
    }

    private async Task GetSessionStorageValues()
    {
        var text = await JS.InvokeAsync<string>("sessionStorage.getItem", "ExecutionsSessionStorage");
        if (text is null) return;
        var sessionStorage = JsonSerializer.Deserialize<SessionStorage>(text, new JsonSerializerOptions { IncludeFields = true });
        ActivePreset = sessionStorage?.Preset;
        (FromDateTime, ToDateTime) = sessionStorage switch
        {
            not null and { Preset: Preset preset } => GetPreset(preset),
            not null => (sessionStorage.FromDateTime, sessionStorage.ToDateTime),
            null => (FromDateTime, ToDateTime)
        };
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

    private enum Preset
    {
        OneHour,
        ThreeHours,
        TwelveHours,
        TwentyFourHours,
        ThreeDays,
        SevenDays,
        FourteenDays,
        ThirtyDays,
        ThisDay,
        ThisWeek,
        ThisMonth,
        PreviousDay,
        PreviousWeek,
        PreviousMonth
    }
}
