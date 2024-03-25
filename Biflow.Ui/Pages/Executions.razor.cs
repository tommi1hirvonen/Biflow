namespace Biflow.Ui.Pages;

[Route("/executions")]
public partial class Executions : ComponentBase, IDisposable
{
    [Inject] private IMediator Mediator { get; set; } = null!;

    [CascadingParameter] UserState UserState { get; set; } = null!;

    private readonly CancellationTokenSource cts = new();
    
    private bool loading = false;
    private IEnumerable<ExecutionProjection>? executions;
    private IEnumerable<StepExecutionProjection>? stepExecutions;

    private IEnumerable<ExecutionProjection>? FilteredExecutions => executions?
        .Where(e => UserState.Executions.JobStatusFilter.Count == 0 || UserState.Executions.JobStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => UserState.Executions.JobFilter.Count == 0 || UserState.Executions.JobFilter.Contains(e.JobName))
        .Where(e => UserState.Executions.JobTagFilter.Count == 0 || e.Tags.Any(t => UserState.Executions.JobTagFilter.Contains(t)))
        .Where(e => UserState.Executions.StartTypeFilter == StartType.All ||
        UserState.Executions.StartTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
        UserState.Executions.StartTypeFilter == StartType.Manual && e.ScheduleId is null);

    private IEnumerable<StepExecutionProjection>? FilteredStepExecutions => stepExecutions?
        .Where(e => UserState.Executions.StartTypeFilter == StartType.All ||
        UserState.Executions.StartTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
        UserState.Executions.StartTypeFilter == StartType.Manual && e.ScheduleId is null)
        .Where(e => UserState.Executions.StepTagFilter.Count == 0 || e.StepTags.Any(t => UserState.Executions.StepTagFilter.Contains(t)))
        .Where(e => UserState.Executions.JobTagFilter.Count == 0 || e.JobTags.Any(t => UserState.Executions.JobTagFilter.Contains(t)))
        .Where(e => UserState.Executions.StepStatusFilter.Count == 0 || UserState.Executions.StepStatusFilter.Contains(e.ExecutionStatus))
        .Where(e => UserState.Executions.JobFilter.Count == 0 || UserState.Executions.JobFilter.Contains(e.JobName))
        .Where(e => UserState.Executions.StepFilter.Count == 0 || UserState.Executions.StepFilter.Contains((e.StepName, e.StepType)))
        .Where(e => UserState.Executions.StepTypeFilter.Count == 0 || UserState.Executions.StepTypeFilter.Contains(e.StepType));

    protected override async Task OnInitializedAsync()
    {
        if (UserState.Executions.Preset is Preset preset)
        {
            (UserState.Executions.FromDateTime, UserState.Executions.ToDateTime) = GetPreset(preset);
        }
        await LoadDataAsync();
    }

    private async Task ShowExecutionsAsync()
    {
        executions = null;
        UserState.Executions.ShowSteps = false;
        await LoadDataAsync();
    }

    private async Task ShowStepExecutionsAsync()
    {
        executions = null;
        UserState.Executions.ShowSteps = true;
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        loading = true;
        StateHasChanged();

        if (UserState.Executions.Preset is Preset preset)
        {
            (UserState.Executions.FromDateTime, UserState.Executions.ToDateTime) = GetPreset(preset);
        }

        if (!UserState.Executions.ShowSteps)
        {
            var request = new ExecutionsMonitoringQuery(UserState.Executions.FromDateTime, UserState.Executions.ToDateTime);
            var response = await Mediator.SendAsync(request, cts.Token);
            executions = response.Executions;
        }
        else
        {
            var request = new StepExecutionsMonitoringQuery(UserState.Executions.FromDateTime, UserState.Executions.ToDateTime);
            var response = await Mediator.SendAsync(request, cts.Token);
            stepExecutions = response.Executions;
        }

        loading = false;
        StateHasChanged();
    }

    private async Task ApplyPresetAsync(Preset preset)
    {
        (UserState.Executions.FromDateTime, UserState.Executions.ToDateTime) = GetPreset(preset);
        UserState.Executions.Preset = preset;
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
            _ => (UserState.Executions.FromDateTime, UserState.Executions.ToDateTime)
        };
    }

    private void ClearFilters()
    {
        UserState.Executions.JobStatusFilter.Clear();
        UserState.Executions.StepStatusFilter.Clear();
        UserState.Executions.JobFilter.Clear();
        UserState.Executions.JobTagFilter.Clear();
        UserState.Executions.StepFilter.Clear();
        UserState.Executions.StepTypeFilter.Clear();
        UserState.Executions.StepTagFilter.Clear();
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

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}
