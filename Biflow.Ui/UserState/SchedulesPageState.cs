namespace Biflow.Ui.StateManagement;

public class SchedulesPageState
{
    public HashSet<(Guid JobId, string JobName)> JobFilter { get; } = [];

    public StateFilter StateFilter { get; set; } = StateFilter.All;

    public int PageSize { get; set; } = 25;

    public int CurrentPage { get; set; } = 1;
}
