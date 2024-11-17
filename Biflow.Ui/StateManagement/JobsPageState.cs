namespace Biflow.Ui.StateManagement;

public class JobsPageState
{
    public string JobNameFilter { get; set; } = "";

    public string StepNameFilter { get; set; } = "";

    public HashSet<ExecutionStatus> StatusFilter { get; } = [];

    public HashSet<TagProjection> TagFilter { get; } = [];

    public StateFilter StateFilter { get; set; } = StateFilter.All;

    public JobSortMode SortMode { get; set; } = JobSortMode.NameAsc;

    public int PageSize { get; set; } = 20;

    public int CurrentPage { get; set; } = 1;
}