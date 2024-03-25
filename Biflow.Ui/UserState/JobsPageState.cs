namespace Biflow.Ui.StateManagement;

public class JobsPageState
{
    public string JobNameFilter = "";

    public string StepNameFilter = "";

    public HashSet<ExecutionStatus> StatusFilter = [];

    public HashSet<TagProjection> TagFilter = [];

    public StateFilter StateFilter { get; set; } = StateFilter.All;

    public JobSortMode SortMode { get; set; } = JobSortMode.NameAsc;

    public int PageSize { get; set; } = 25;

    public int CurrentPage { get; set; } = 1;
}