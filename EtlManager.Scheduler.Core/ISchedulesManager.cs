using EtlManager.DataAccess.Models;

namespace EtlManager.Scheduler.Core;

public interface ISchedulesManager
{
    public Task AddScheduleAsync(Schedule schedule, CancellationToken cancellationToken);
    
    public Task PauseScheduleAsync(Schedule schedule, CancellationToken cancellationToken);
    
    public Task ReadAllSchedules(CancellationToken cancellationToken);

    public Task RemoveJobAsync(Job job, CancellationToken cancellationToken);
    
    public Task RemoveScheduleAsync(Schedule schedule, CancellationToken cancellationToken);
    
    public Task ResumeScheduleAsync(Schedule schedule, CancellationToken cancellationToken);
}
