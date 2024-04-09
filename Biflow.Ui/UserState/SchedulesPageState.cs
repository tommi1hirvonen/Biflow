namespace Biflow.Ui.StateManagement;

public class SchedulesPageState
{
    public HashSet<(Guid JobId, string JobName)> JobFilter { get; } = [];

    public HashSet<TagProjection> JobTagFilter { get; } = [];

    public HashSet<TagProjection> ScheduleTagFilter { get; } = [];

    public string ScheduleFilter { get; set; } = "";

    public StateFilter StateFilter { get; set; } = StateFilter.All;

    public int PageSize { get; set; } = 20;

    public int CurrentPage { get; set; } = 1;

    public DateTime? TriggersAfter
    {
        get => triggersAfter;
        set
        {
            triggersAfter = value;
            if (triggersBefore < value)
            {
                triggersBefore = value;
            }
        }
    }

    private DateTime? triggersAfter;

    public DateTime? TriggersBefore
    {
        get => triggersBefore;
        set
        {
            triggersBefore = value;
            if (triggersAfter > value)
            {
                triggersAfter = value;
            }
        }
    }

    private DateTime? triggersBefore;

    public void SetTriggersInNext(TimeSpan timeSpan)
    {
        TriggersAfter = DateTime.Now;
        TriggersBefore = DateTime.Now.Add(timeSpan);
    }

    public void Clear()
    {
        JobFilter.Clear();
        JobTagFilter.Clear();
        ScheduleTagFilter.Clear();
        ScheduleFilter = "";
        StateFilter = StateFilter.All;
        triggersAfter = null;
        triggersBefore = null;
    }
}
