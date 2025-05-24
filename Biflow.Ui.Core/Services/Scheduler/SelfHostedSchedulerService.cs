using Biflow.Scheduler.Core;

namespace Biflow.Ui.Core;

public class SelfHostedSchedulerService(ISchedulesManager schedulesManager) : ISchedulerService
{
    private readonly ISchedulesManager _schedulesManager = schedulesManager;

    public async Task AddScheduleAsync(Schedule schedule)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        await _schedulesManager.AddScheduleAsync(schedulerSchedule, CancellationToken.None);
    }

    public async Task RemoveScheduleAsync(Schedule schedule)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        await _schedulesManager.RemoveScheduleAsync(schedulerSchedule, CancellationToken.None);
    }

    public async Task UpdateScheduleAsync(Schedule schedule)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        await _schedulesManager.UpdateScheduleAsync(schedulerSchedule, CancellationToken.None);
    }

    public async Task DeleteJobAsync(Guid jobId)
    {
        var schedulerJob = new SchedulerJob(jobId);
        await _schedulesManager.RemoveJobAsync(schedulerJob, CancellationToken.None);
    }

    public async Task<SchedulerStatusResponse> GetStatusAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        SchedulerStatusResponse response = _schedulesManager.DatabaseReadException is not null
            ? new SchedulerError()
            : new Success(await _schedulesManager.GetStatusAsync(cts.Token));
        return response;
    }

    public Task SynchronizeAsync() => _schedulesManager.ReadAllSchedulesAsync(CancellationToken.None);

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        if (enabled)
        {
            await _schedulesManager.ResumeScheduleAsync(schedulerSchedule, CancellationToken.None);
        }
        else
        {
            await _schedulesManager.PauseScheduleAsync(schedulerSchedule, CancellationToken.None);
        }
    }

}
