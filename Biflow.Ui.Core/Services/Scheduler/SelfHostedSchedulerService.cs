using Biflow.Core.Entities;
using Biflow.Scheduler.Core;

namespace Biflow.Ui.Core;

public class SelfHostedSchedulerService(ISchedulesManager schedulesManager) : ISchedulerService
{
    private readonly ISchedulesManager _schedulesManager = schedulesManager;

    private bool DatabaseReadError { get; set; } = false;

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
        SchedulerStatusResponse response = DatabaseReadError
            ? new SchedulerError()
            : new Success(await _schedulesManager.GetStatusAsync(cts.Token));
        return response;
    }

    public async Task SynchronizeAsync()
    {
        try
        {
            await _schedulesManager.ReadAllSchedules(CancellationToken.None);
            DatabaseReadError = false;
        }
        catch
        {
            DatabaseReadError = true;
            throw;
        }
    }

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(schedule.JobId);
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
