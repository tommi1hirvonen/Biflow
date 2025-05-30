﻿namespace Biflow.Ui.Pages;

[Route("/executions")]
public partial class Executions(IMediator mediator, ToasterService toaster) : ComponentBase, IDisposable
{
    [CascadingParameter] public UserState UserState { get; set; } = null!;

    private ExecutionsPageState State => UserState.Executions;

    private readonly IMediator _mediator = mediator;
    private readonly ToasterService _toaster = toaster;
    private readonly CancellationTokenSource _cts = new();
    
    private bool _loading;
    private IEnumerable<ExecutionProjection>? _executions;
    private IEnumerable<StepExecutionProjection>? _stepExecutions;
    private Paginator<ExecutionProjection>? _executionPaginator;
    private Paginator<StepExecutionProjection>? _stepExecutionPaginator;
    private HxOffcanvas? _deleteOffcanvas;
    private DateTime _deleteFrom = new(2000, 1, 1);
    private DateTime _deleteTo = DateTime.Now.AddYears(-1);

    protected override async Task OnInitializedAsync()
    {
        if (State.Preset is { } preset)
        {
            (State.FromDateTime, State.ToDateTime) = GetPreset(preset);
        }
        await LoadDataAsync();
    }

    private IEnumerable<ExecutionProjection>? GetOrderedExecutions()
    {
        var filtered = _executions?.Where(e => State.ExecutionPredicates.All(p => p(e)));
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

    private IEnumerable<StepExecutionProjection>? GetOrderedStepExecutions()
    {
        var filtered = _stepExecutions?.Where(e => State.StepExecutionPredicates.All(p => p(e)));
        return UserState.Executions.StepExecutionSortMode switch
        {
            StepExecutionSortMode.CreatedDesc => filtered?.OrderByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            StepExecutionSortMode.JobAsc => filtered?.OrderBy(e => e.JobName).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            StepExecutionSortMode.JobDesc => filtered?.OrderByDescending(e => e.JobName).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            StepExecutionSortMode.StepAsc => filtered?.OrderBy(e => e.StepName).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            StepExecutionSortMode.StepDesc => filtered?.OrderByDescending(e => e.StepName).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            StepExecutionSortMode.StartedAsc => filtered?.OrderBy(e => e.StartedOn),
            StepExecutionSortMode.StartedDesc => filtered?.OrderByDescending(e => e.StartedOn),
            StepExecutionSortMode.EndedAsc => filtered?.OrderBy(e => e.EndedOn),
            StepExecutionSortMode.EndedDesc => filtered?.OrderByDescending(e => e.EndedOn),
            StepExecutionSortMode.DurationAsc => filtered?.OrderBy(e => e.ExecutionInSeconds).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            StepExecutionSortMode.DurationDesc => filtered?.OrderByDescending(e => e.ExecutionInSeconds).ThenByDescending(e => e.CreatedOn).ThenByDescending(e => e.StartedOn),
            _ => filtered
        };
    }

    private async Task ShowExecutionsAsync()
    {
        _executions = null;
        State.ShowSteps = false;
        await LoadDataAsync();
    }

    private async Task ShowStepExecutionsAsync()
    {
        _executions = null;
        State.ShowSteps = true;
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _loading = true;
        StateHasChanged();

        if (State.Preset is { } preset)
        {
            (State.FromDateTime, State.ToDateTime) = GetPreset(preset);
        }

        if (!State.ShowSteps)
        {
            var request = new ExecutionsMonitoringQuery(State.FromDateTime, State.ToDateTime);
            var response = await _mediator.SendAsync(request, _cts.Token);
            _executions = response.Executions;
        }
        else
        {
            var request = new StepExecutionsMonitoringQuery(State.FromDateTime, State.ToDateTime);
            var response = await _mediator.SendAsync(request, _cts.Token);
            _stepExecutions = response.Executions;
        }

        _loading = false;
        StateHasChanged();
    }

    private async Task DeleteExecutionsAsync()
    {
        try
        {
            var command = new DeleteExecutionsCommand(_deleteFrom, _deleteTo);
            await _mediator.SendAsync(command);
            _toaster.AddSuccess("Executions deleted successfully");
        }
        catch (Exception ex)
        {
            _toaster.AddError("Error deleting executions", ex.Message);
        }
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
        _cts.Cancel();
        _cts.Dispose();
    }
}
