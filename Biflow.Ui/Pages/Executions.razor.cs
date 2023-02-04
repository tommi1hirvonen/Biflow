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

        if (ActivePreset is Preset preset)
        {
            (FromDateTime, ToDateTime) = GetPreset(preset);
        }

        using var context = await Task.Run(DbContextFactory.CreateDbContext);

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
            .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.InheritFromExecutionParameter)}")
            .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
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

    private async Task ApplyPresetAsync(Preset preset)
    {
        (FromDateTime, ToDateTime) = GetPreset(preset);
        ActivePreset = preset;
        await LoadData();
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
        HashSet<string> StepNames,
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
        var text = JsonSerializer.Serialize(sessionStorage);
        await JS.InvokeVoidAsync("sessionStorage.setItem", "ExecutionsSessionStorage", text);
    }

    private async Task GetSessionStorageValues()
    {
        var text = await JS.InvokeAsync<string>("sessionStorage.getItem", "ExecutionsSessionStorage");
        if (text is null) return;
        var sessionStorage = JsonSerializer.Deserialize<SessionStorage>(text);
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
