namespace Biflow.Ui.StateManagement;

public class ExecutionsPageState
{
    public bool ShowSteps { get; set; } = false;
    
    public bool ShowGraph { get; set; } = false;

    public Preset? Preset { get; set; } = StateManagement.Preset.OneHour;

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
}
