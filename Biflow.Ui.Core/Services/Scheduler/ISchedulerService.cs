namespace Biflow.Ui.Core;

public interface ISchedulerService
{
    public Task DeleteJobAsync(Guid jobId);

    public Task<SchedulerStatusResponse> GetStatusAsync();

    public Task AddScheduleAsync(Schedule schedule);

    public Task RemoveScheduleAsync(Schedule schedule);

    public Task UpdateScheduleAsync(Schedule schedule);

    public Task SynchronizeAsync();

    public Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled);
}