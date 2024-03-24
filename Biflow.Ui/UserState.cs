namespace Biflow.Ui;

public class UserState
{
    public JobsPageState Jobs { get; } = new();

    public SchedulesPageState Schedules { get; } = new();

    public Dictionary<Guid, ExpandStatus> DataTableCategoryExpandStatuses { get; } = [];
}

public class ExpandStatus
{
    public bool IsExpanded { get; set; } = true;
}

public class JobsPageState
{
    public string JobNameFilter = "";

    public string StepNameFilter = "";

    public HashSet<ExecutionStatus> StatusFilter = [];

    public StateFilter StateFilter { get; set; } = StateFilter.All;

    public JobSortMode SortMode { get; set; } = JobSortMode.NameAsc;

    public int PageSize { get; set; } = 25;

    public int CurrentPage { get; set; } = 1;
}

public class SchedulesPageState
{
    public HashSet<(Guid JobId, string JobName)> JobFilter { get; } = [];

    public StateFilter StateFilter { get; set; } = StateFilter.All;

    public int PageSize { get; set; } = 25;

    public int CurrentPage { get; set; } = 1;
}

public enum StateFilter { All, Enabled, Disabled }

public enum JobSortMode { NameAsc, NameDesc, LastExecAsc, LastExecDesc, NextExecAsc, NextExecDesc }