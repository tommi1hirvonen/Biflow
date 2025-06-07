namespace Biflow.Ui.Core;

public class SchedulerModeResolver
{
    internal SchedulerModeResolver(SchedulerMode schedulerMode)
    {
        SchedulerMode = schedulerMode;
    }

    public SchedulerMode SchedulerMode { get; }
}