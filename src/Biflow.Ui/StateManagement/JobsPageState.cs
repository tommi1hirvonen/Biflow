namespace Biflow.Ui.StateManagement;

public class JobsPageState
{
    public string JobNameFilter { get; set; } = "";

    public string StepNameFilter { get; set; } = "";

    public HashSet<ExecutionStatus> StatusFilter { get; } = [];

    public HashSet<TagProjection> TagFilter { get; } = [];

    public StateFilter StateFilter { get; set; } = StateFilter.All;

    public JobSortMode SortMode { get; private set; } = JobSortMode.Pinned;

    public int PageSize { get; set; } = 20;

    public int CurrentPage { get; set; } = 1;

    public void ToggleSortName()
    {
        SortMode = SortMode switch
        {
            JobSortMode.NameDesc => JobSortMode.Pinned,
            JobSortMode.NameAsc => JobSortMode.NameDesc,
            _ => JobSortMode.NameAsc
        };
    }
    
    public void ToggleSortLastExec()
    {
        SortMode = SortMode switch
        {
            JobSortMode.LastExecDesc => JobSortMode.Pinned,
            JobSortMode.LastExecAsc => JobSortMode.LastExecDesc,
            _ => JobSortMode.LastExecAsc
        };
    }
    
    public void ToggleSortNextExec()
    {
        SortMode = SortMode switch
        {
            JobSortMode.NextExecDesc => JobSortMode.Pinned,
            JobSortMode.NextExecAsc => JobSortMode.NextExecDesc,
            _ => JobSortMode.NextExecAsc
        };
    }
}