using Mono.TextTemplating;
using static Havit.Blazor.Components.Web.Bootstrap.HxListLayout;

namespace Biflow.Ui.StateManagement;

public class SchedulesPageState
{
    public SchedulesPageState()
    {
        JobPredicate = s => JobFilter.Count == 0 || JobFilter.Any(f => f.JobId == s.JobId);
        JobTagPredicate = s => JobTagFilter.Count == 0 || JobTagFilter.Any(f => s.Job.Tags.Any(t => t.TagId == f.TagId));
        ScheduleTagPredicate = s => ScheduleTagFilter.Count == 0 || ScheduleTagFilter.Any(f => s.Tags.Any(t => t.TagId == f.TagId));
        ScheduleNamePredicate = s => string.IsNullOrEmpty(ScheduleFilter) || s.ScheduleName.ContainsIgnoreCase(ScheduleFilter);

        Predicates =
        [
            JobPredicate,
            JobTagPredicate,
            ScheduleTagPredicate,
            ScheduleNamePredicate
        ];
}

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

    public Func<Schedule, bool> JobPredicate { get; }
    
    public Func<Schedule, bool> JobTagPredicate { get; }
    
    public Func<Schedule, bool> ScheduleTagPredicate { get; }
    
    public Func<Schedule, bool> ScheduleNamePredicate { get; }

    public IEnumerable<Func<Schedule, bool>> Predicates { get; }

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
