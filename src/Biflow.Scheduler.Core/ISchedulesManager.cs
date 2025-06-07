using Microsoft.Extensions.Hosting;

namespace Biflow.Scheduler.Core;

public interface ISchedulesManager : IHostedService
{
    internal Exception? DatabaseReadException { get; }

    public Task AddScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
    
    public Task<IEnumerable<JobStatus>> GetStatusAsync(CancellationToken cancellationToken);
    
    public Task PauseScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
    
    public Task ReadAllSchedulesAsync(CancellationToken cancellationToken);

    public Task RemoveJobAsync(SchedulerJob job, CancellationToken cancellationToken);
    
    public Task RemoveScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
    
    public Task ResumeScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
    
    public Task UpdateScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken);
}
