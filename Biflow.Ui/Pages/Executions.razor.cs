namespace Biflow.Ui.Pages;

[Route("/executions")]
public partial class Executions : ComponentBase, IDisposable
{
    [Inject] private IMediator Mediator { get; set; } = null!;

    [CascadingParameter] UserState UserState { get; set; } = null!;

    private ExecutionsPageState State => UserState.Executions;

    private readonly CancellationTokenSource cts = new();
    
    private bool loading = false;
    private IEnumerable<ExecutionProjection>? executions;
    private IEnumerable<StepExecutionProjection>? stepExecutions;
    private Paginator<ExecutionProjection>? executionPaginator;
    private Paginator<StepExecutionProjection>? stepExecutionPaginator;

    private IEnumerable<StepExecutionProjection>? FilteredStepExecutions => stepExecutions?
        .Where(e => State.StepExecutionPredicates.All(p => p(e)));

    protected override async Task OnInitializedAsync()
    {
        if (State.Preset is Preset preset)
        {
            (State.FromDateTime, State.ToDateTime) = GetPreset(preset);
        }
        await LoadDataAsync();
    }

    private IEnumerable<ExecutionProjection>? GetOrderedExecutions()
    {
        var filtered = executions?.Where(e => State.ExecutionPredicates.All(p => p(e)));
        return UserState.Executions.ExecutionSortMode switch
        {
            ExecutionSortMode.CreatedDesc => filtered?.OrderByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            ExecutionSortMode.JobAsc => filtered?.OrderBy(e => e.JobName).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            ExecutionSortMode.JobDesc => filtered?.OrderByDescending(e => e.JobName).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            ExecutionSortMode.StartedAsc => filtered?.OrderBy(e => e.StartedOn),
            ExecutionSortMode.StartedDesc => filtered?.OrderByDescending(e => e.StartedOn),
            ExecutionSortMode.EndedAsc => filtered?.OrderBy(e => e.EndedOn),
            ExecutionSortMode.EndedDesc => filtered?.OrderByDescending(e => e.EndedOn),
            ExecutionSortMode.DurationAsc => filtered?.OrderBy(e => e.ExecutionInSeconds).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            ExecutionSortMode.DurationDesc => filtered?.OrderByDescending(e => e.ExecutionInSeconds).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            _ => filtered
        };
    }

    private async Task ShowExecutionsAsync()
    {
        executions = null;
        State.ShowSteps = false;
        await LoadDataAsync();
    }

    private async Task ShowStepExecutionsAsync()
    {
        executions = null;
        State.ShowSteps = true;
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        loading = true;
        StateHasChanged();

        if (State.Preset is Preset preset)
        {
            (State.FromDateTime, State.ToDateTime) = GetPreset(preset);
        }

        if (!State.ShowSteps)
        {
            var request = new ExecutionsMonitoringQuery(State.FromDateTime, State.ToDateTime);
            var response = await Mediator.SendAsync(request, cts.Token);
            executions = response.Executions;
        }
        else
        {
            var request = new StepExecutionsMonitoringQuery(State.FromDateTime, State.ToDateTime);
            var response = await Mediator.SendAsync(request, cts.Token);
            stepExecutions = response.Executions;
        }

        loading = false;
        StateHasChanged();
    }

    private async Task ApplyPresetAsync(Preset preset)
    {
        (State.FromDateTime, State.ToDateTime) = GetPreset(preset);
        State.Preset = preset;
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
            _ => (State.FromDateTime, State.ToDateTime)
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

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}
