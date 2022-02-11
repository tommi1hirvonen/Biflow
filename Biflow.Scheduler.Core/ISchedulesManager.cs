namespace Biflow.Scheduler.Core;

public interface ISchedulesManager
{
    public Task AddScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
    
    public Task PauseScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
    
    public Task ReadAllSchedules(CancellationToken cancellationToken);

    public Task RemoveJobAsync(SchedulerJob job, CancellationToken cancellationToken);
    
    public Task RemoveScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
    
    public Task ResumeScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
}
