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
    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;

    private bool sessionStorageRetrieved = false;
    private bool showSteps = false;
    private bool showGraph = false;
    private bool loading = false;
    private Preset? activePreset = Preset.OneHour;
    private IEnumerable<ExecutionProjection>? executions;
    private IEnumerable<StepExecutionProjection>? stepExecutions;
    private HashSet<ExecutionStatus> jobStatusFilter = [];
    private HashSet<StepExecutionStatus> stepStatusFilter = [];
    private HashSet<string> jobFilter = [];
    private HashSet<(string StepName, StepType StepType)> stepFilter = [];
    private HashSet<StepType> stepTypeFilter = [];
    private HashSet<string> tagFilter = [];
    private StartType startTypeFilter = StartType.All;

    private static readonly JsonSerializerOptions SerializerOptions = new() { IncludeFields = true };

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

    private IEnumerable<ExecutionProjection>? FilteredExecutions => executions?
        .Where(e => jobStatusFilter.Count == 0 || jobStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => jobFilter.Count == 0 || jobFilter.Contains(e.JobName))
        .Where(e => startTypeFilter == StartType.All ||
        startTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
        startTypeFilter == StartType.Manual && e.ScheduleId is null);

    private IEnumerable<StepExecutionProjection>? FilteredStepExecutions => stepExecutions?
        .Where(e => startTypeFilter == StartType.All ||
        startTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
        startTypeFilter == StartType.Manual && e.ScheduleId is null)
        .Where(e => tagFilter.Count == 0 || e.Tags.Any(t => tagFilter.Contains(t.TagName)) == true)
        .Where(e => stepStatusFilter.Count == 0 || stepStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => jobFilter.Count == 0 || jobFilter.Contains(e.JobName))
        .Where(e => stepFilter.Count == 0 || stepFilter.Contains((e.StepName, e.StepType)))
        .Where(e => stepTypeFilter.Count == 0 || stepTypeFilter.Contains(e.StepType));

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

            sessionStorageRetrieved = true;
            StateHasChanged();
            await LoadDataAsync();
        }
    }

    private async Task ShowExecutionsAsync()
    {
        executions = null;
        showSteps = false;
        await LoadDataAsync();
    }

    private async Task ShowStepExecutionsAsync()
    {
        executions = null;
        showSteps = true;
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        loading = true;
        StateHasChanged();

        if (activePreset is Preset preset)
        {
            (FromDateTime, ToDateTime) = GetPreset(preset);
        }

        using var context = await Task.Run(DbContextFactory.CreateDbContext);

        if (!showSteps)
        {
            var query = context.Executions
                .AsNoTracking()
                .AsSingleQuery()
                // Index optimized way of querying executions without having to scan the entire table.
                .Where(e => e.CreatedOn <= ToDateTime && e.EndedOn >= FromDateTime);

            if (DateTime.Now >= FromDateTime && DateTime.Now <= ToDateTime)
            {
                query = query
                    .Concat(context.Executions
                        // Adds executions that were not started.
                        .Where(e => e.CreatedOn >= FromDateTime && e.CreatedOn <= ToDateTime && e.EndedOn == null && e.ExecutionStatus != ExecutionStatus.Running))
                    .Concat(context.Executions
                        // Adds currently running executions if current time fits in the time window.
                        .Where(e => e.ExecutionStatus == ExecutionStatus.Running));
            }
            else
            {
                query = query
                    .Concat(context.Executions
                        // Adds executions that were not started and executions that may still be running.
                        .Where(e => e.CreatedOn >= FromDateTime && e.CreatedOn <= ToDateTime && e.EndedOn == null));
            }
            executions = await (
                from e in query
                join job in context.Jobs on e.JobId equals job.JobId into ej
                from job in ej.DefaultIfEmpty() // Translates to left join in SQL
                orderby e.CreatedOn descending, e.StartedOn descending
                select new ExecutionProjection(
                    e.ExecutionId,
                    e.JobId,
                    job.JobName ?? e.JobName,
                    e.ScheduleId,
                    e.CreatedOn,
                    e.StartedOn,
                    e.EndedOn,
                    e.ExecutionStatus,
                    e.StepExecutions.Count()
                )).ToArrayAsync();
        }
        else
        {
            var query = context.StepExecutionAttempts
                .AsNoTracking()
                .Where(e => e.StepExecution.Execution.CreatedOn <= ToDateTime && e.StepExecution.Execution.EndedOn >= FromDateTime);

            if (DateTime.Now >= FromDateTime && DateTime.Now <= ToDateTime)
            {
                query = query
                    // Adds executions that were not started.
                    .Union(context.StepExecutionAttempts
                        .Where(e => e.StepExecution.Execution.CreatedOn >= FromDateTime
                        && e.StepExecution.Execution.CreatedOn <= ToDateTime
                        && e.EndedOn == null
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
                        .Where(e => e.StepExecution.Execution.CreatedOn >= FromDateTime
                        && e.StepExecution.Execution.CreatedOn <= ToDateTime
                        && e.EndedOn == null));
            }
            stepExecutions = await (
                from e in query
                join job in context.Jobs on e.StepExecution.Execution.JobId equals job.JobId into j
                from job in j.DefaultIfEmpty()
                join step in context.Steps on e.StepId equals step.StepId into s
                from step in s.DefaultIfEmpty()
                orderby e.StepExecution.Execution.CreatedOn descending, e.StartedOn descending, e.StepExecution.ExecutionPhase descending
                select new StepExecutionProjection(
                    e.StepExecution.ExecutionId,
                    e.StepExecution.StepId,
                    e.RetryAttemptIndex,
                    step.StepName ?? e.StepExecution.StepName,
                    e.StepType,
                    e.StepExecution.ExecutionPhase,
                    e.StartedOn,
                    e.EndedOn,
                    e.ExecutionStatus,
                    e.StepExecution.Execution.ExecutionStatus,
                    e.StepExecution.Execution.DependencyMode,
                    e.StepExecution.Execution.ScheduleId,
                    e.StepExecution.Execution.JobId,
                    job.JobName ?? e.StepExecution.Execution.JobName,
                    step.Tags.ToArray()
                )).ToArrayAsync();
        }

        loading = false;
        StateHasChanged();
    }

    private async Task ApplyPresetAsync(Preset preset)
    {
        (FromDateTime, ToDateTime) = GetPreset(preset);
        activePreset = preset;
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
            Preset: activePreset,
            FromDateTime: FromDateTime,
            ToDateTime: ToDateTime,
            ShowSteps: showSteps,
            ShowGraph: showGraph,
            StartType: startTypeFilter,
            JobStatuses: jobStatusFilter,
            StepStatuses: stepStatusFilter,
            JobNames: jobFilter,
            StepNames: stepFilter,
            StepTypes: stepTypeFilter,
            Tags: tagFilter
        );
        var text = JsonSerializer.Serialize(sessionStorage, SerializerOptions);
        await JS.InvokeVoidAsync("sessionStorage.setItem", "ExecutionsSessionStorage", text);
    }

    private async Task GetSessionStorageValues()
    {
        var text = await JS.InvokeAsync<string>("sessionStorage.getItem", "ExecutionsSessionStorage");
        if (text is null) return;
        var sessionStorage = JsonSerializer.Deserialize<SessionStorage>(text, SerializerOptions);
        activePreset = sessionStorage?.Preset;
        (FromDateTime, ToDateTime) = sessionStorage switch
        {
            not null and { Preset: Preset preset } => GetPreset(preset),
            not null => (sessionStorage.FromDateTime, sessionStorage.ToDateTime),
            null => (FromDateTime, ToDateTime)
        };
        showSteps = sessionStorage?.ShowSteps ?? showSteps;
        showGraph = sessionStorage?.ShowGraph ?? showGraph;
        startTypeFilter = sessionStorage?.StartType ?? startTypeFilter;
        jobStatusFilter = sessionStorage?.JobStatuses ?? jobStatusFilter;
        stepStatusFilter = sessionStorage?.StepStatuses ?? stepStatusFilter;
        jobFilter = sessionStorage?.JobNames ?? jobFilter;
        stepFilter = sessionStorage?.StepNames ?? stepFilter;
        stepTypeFilter = sessionStorage?.StepTypes ?? stepTypeFilter;
        tagFilter = sessionStorage?.Tags ?? tagFilter;
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
