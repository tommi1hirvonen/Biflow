using EtlManager.Utilities;

namespace EtlManager.Scheduler.Core;

public interface ISchedulesManager
{
    public Task AddScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken);
    
    public Task PauseScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken);
    
    public Task ReadAllSchedules(CancellationToken cancellationToken);
    
    public Task RemoveScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken);
    
    public Task ResumeScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken);
}
