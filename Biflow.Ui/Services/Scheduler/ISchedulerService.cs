using Biflow.DataAccess.Models;

namespace Biflow.Ui;

public interface ISchedulerService
{
    public Task DeleteJobAsync(Job job);

    public Task<(bool SchedulerDetected, bool SchedulerError)> GetStatusAsync();

    public Task AddScheduleAsync(Schedule schedule);

    public Task RemoveScheduleAsync(Schedule schedule);

    public Task SynchronizeAsync();

    public Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled);
}
