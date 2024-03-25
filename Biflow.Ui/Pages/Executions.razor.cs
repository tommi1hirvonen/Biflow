using Microsoft.JSInterop;
using System.Text.Json;

namespace Biflow.Ui.Pages;

[Route("/executions")]
public partial class Executions : ComponentBase, IDisposable, IAsyncDisposable
{
    [Inject] private IDbContextFactory<AppDbContext> DbContextFactory { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ToasterService Toaster { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;

    private readonly CancellationTokenSource cts = new();

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
    private HashSet<string> jobTagFilter = [];
    private HashSet<string> stepTagFilter = [];
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
        .Where(e => jobTagFilter.Count == 0 || e.Tags.Any(t => jobTagFilter.Contains(t.TagName)) == true)
        .Where(e => startTypeFilter == StartType.All ||
        startTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
        startTypeFilter == StartType.Manual && e.ScheduleId is null);

    private IEnumerable<StepExecutionProjection>? FilteredStepExecutions => stepExecutions?
        .Where(e => startTypeFilter == StartType.All ||
        startTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
        startTypeFilter == StartType.Manual && e.ScheduleId is null)
        .Where(e => stepTagFilter.Count == 0 || e.StepTags.Any(t => stepTagFilter.Contains(t.TagName)) == true)
        .Where(e => jobTagFilter.Count == 0 || e.JobTags.Any(t => jobTagFilter.Contains(t.TagName)) == true)
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
                Toaster.AddWarning("Error getting session storage values", ex.Message);
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

        if (!showSteps)
        {
            var request = new ExecutionsMonitoringQuery(FromDateTime, ToDateTime);
            var response = await Mediator.SendAsync(request, cts.Token);
            executions = response.Executions;
        }
        else
        {
            var request = new StepExecutionsMonitoringQuery(FromDateTime, ToDateTime);
            var response = await Mediator.SendAsync(request, cts.Token);
            stepExecutions = response.Executions;
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
        HashSet<string> StepTags,
        HashSet<string> JobTags
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
            StepTags: stepTagFilter,
            JobTags: jobTagFilter
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
        stepTagFilter = sessionStorage?.StepTags ?? stepTagFilter;
        jobTagFilter = sessionStorage?.JobTags ?? jobTagFilter;
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await SetSessionStorageValues();
        }
        catch (Exception ex)
        {
            Toaster.AddWarning("Error saving session storage values", ex.Message);
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
