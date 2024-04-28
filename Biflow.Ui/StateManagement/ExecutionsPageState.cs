namespace Biflow.Ui.StateManagement;

public class ExecutionsPageState
{
    public ExecutionsPageState()
    {
        JobStatusPredicate = e => JobStatusFilter.Count == 0 || JobStatusFilter.Contains(e.ExecutionStatus);
        JobPredicate = e => JobFilter.Count == 0 || JobFilter.Contains(e.JobName);
        JobTagPredicate = e=> JobTagFilter.Count == 0 || e.JobTags.Any(t => JobTagFilter.Contains(t));
        SchedulePredicate = e => ScheduleFilter.Count == 0 || ScheduleFilter.Any(f => f.ScheduleId == e.ScheduleId);
        StartTypePredicate = e => StartTypeFilter == StartType.All ||
                                  StartTypeFilter == StartType.Scheduled && e.ScheduleId is not null ||
                                  StartTypeFilter == StartType.Manual && e.ScheduleId is null;

        ExecutionPredicates =
        [
            JobStatusPredicate,
            JobPredicate,
            JobTagPredicate,
            SchedulePredicate,
            StartTypePredicate
        ];

        StepPredicate = e => StepFilter.Count == 0 || StepFilter.Contains((e.StepName, e.StepType));
        StepStatusPredicate = e => StepStatusFilter.Count == 0 || StepStatusFilter.Contains(e.StepExecutionStatus);
        StepTypePredicate = e => StepTypeFilter.Count == 0 || StepTypeFilter.Contains(e.StepType);
        StepTagPredicate = e => StepTagFilter.Count == 0 || e.StepTags.Any(t => StepTagFilter.Contains(t));

        StepExecutionPredicates =
        [
            JobPredicate,
            JobTagPredicate,
            SchedulePredicate,
            StartTypePredicate,
            StepPredicate,
            StepStatusPredicate,
            StepTypePredicate,
            StepTagPredicate
        ];
    }

    public bool ShowSteps { get; set; } = false;
    
    public bool ShowGraph { get; set; } = false;

    public Preset? Preset { get; set; } = StateManagement.Preset.OneHour;

    public string? PresetText => Preset switch
    {
        StateManagement.Preset.OneHour => "Last 1 h",
        StateManagement.Preset.ThreeHours => "Last 3 h",
        StateManagement.Preset.TwelveHours => "Last 12 h",
        StateManagement.Preset.TwentyFourHours => "Last 24 h",
        StateManagement.Preset.ThreeDays => "Last 3 d",
        StateManagement.Preset.SevenDays => "Last 7 d",
        StateManagement.Preset.FourteenDays => "Last 14 d",
        StateManagement.Preset.ThirtyDays => "Last 30 d",
        StateManagement.Preset.ThisDay => "This day",
        StateManagement.Preset.ThisWeek => "This week",
        StateManagement.Preset.ThisMonth => "This month",
        StateManagement.Preset.PreviousDay => "Previous day",
        StateManagement.Preset.PreviousWeek => "Previous week",
        StateManagement.Preset.PreviousMonth => "Previous month",
        _ => null
    };

    public DateTime FromDateTime
    {
        get => _fromDateTime;
        set => _fromDateTime = value > ToDateTime ? ToDateTime : value;
    }
    private DateTime _fromDateTime = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddHours(-1);

    public DateTime ToDateTime
    {
        get => _toDateTime;
        set => _toDateTime = value < FromDateTime ? FromDateTime : value;
    }
    private DateTime _toDateTime = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddMinutes(1);

    public ExecutionSortMode ExecutionSortMode { get; set; } = ExecutionSortMode.CreatedDesc;

    public StepExecutionSortMode StepExecutionSortMode { get; set; } = StepExecutionSortMode.CreatedDesc;

    public StartType StartTypeFilter { get; set; } = StartType.All;
    
    public HashSet<ExecutionStatus> JobStatusFilter { get; } = [];
    
    public HashSet<StepExecutionStatus> StepStatusFilter { get; } = [];
    
    public HashSet<string> JobFilter { get; } = [];
    
    public HashSet<(string StepName, StepType StepType)> StepFilter { get; } = [];

    public HashSet<ScheduleProjection> ScheduleFilter { get; } = [];
    
    public HashSet<StepType> StepTypeFilter { get; } = [];
    
    public HashSet<TagProjection> StepTagFilter { get; } = [];
    
    public HashSet<TagProjection> JobTagFilter { get; } = [];

    public int PageSize { get; set; } = 20;

    public int ExecutionsCurrentPage { get; set; } = 1;

    public int StepExecutionsCurrentPage { get; set;  } = 1;

    public Predicate<IExecutionProjection> JobStatusPredicate { get; }

    public Predicate<IExecutionProjection> JobPredicate { get; }

    public Predicate<IExecutionProjection> JobTagPredicate { get; }

    public Predicate<IExecutionProjection> SchedulePredicate { get; }

    public Predicate<IExecutionProjection> StartTypePredicate { get; }

    public IEnumerable<Predicate<IExecutionProjection>> ExecutionPredicates { get; }

    public Predicate<StepExecutionProjection> StepPredicate { get; }

    public Predicate<StepExecutionProjection> StepTagPredicate { get; }

    public Predicate<StepExecutionProjection> StepStatusPredicate { get; }

    public Predicate<StepExecutionProjection> StepTypePredicate { get; }

    public IEnumerable<Predicate<StepExecutionProjection>> StepExecutionPredicates { get; }

    public void Clear()
    {
        JobStatusFilter.Clear();
        StepStatusFilter.Clear();
        JobFilter.Clear();
        JobTagFilter.Clear();
        StepFilter.Clear();
        StepTypeFilter.Clear();
        StepTagFilter.Clear();
        ScheduleFilter.Clear();
        StartTypeFilter = StartType.All;
    }
}