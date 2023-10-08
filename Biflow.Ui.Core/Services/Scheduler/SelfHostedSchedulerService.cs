using Biflow.DataAccess.Models;
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

    public async Task DeleteJobAsync(Job job)
    {
        var schedulerJob = new SchedulerJob(job.JobId);
        await _schedulesManager.RemoveJobAsync(schedulerJob, CancellationToken.None);
    }

    public Task<SchedulerStatusResponse> GetStatusAsync()
    {
        SchedulerStatusResponse response = DatabaseReadError ? new SchedulerError() : new Success();
        return Task.FromResult(response);
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
