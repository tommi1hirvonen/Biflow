using Biflow.DataAccess.Models;
using Biflow.Scheduler.Core;

namespace Biflow.Ui.Core;

public class SelfHostedSchedulerService : ISchedulerService
{
    private readonly ISchedulesManager _schedulesManager;

    private bool DatabaseReadError { get; set; } = false;

    public SelfHostedSchedulerService(ISchedulesManager schedulesManager)
    {
        _schedulesManager = schedulesManager;
    }

    public async Task AddScheduleAsync(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule.CronExpression);
        ArgumentNullException.ThrowIfNull(schedule.JobId);
        var schedulerSchedule = new SchedulerSchedule(schedule.ScheduleId, (Guid)schedule.JobId, schedule.CronExpression);
        await _schedulesManager.AddScheduleAsync(schedulerSchedule, CancellationToken.None);
    }

    public async Task RemoveScheduleAsync(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule.CronExpression);
        ArgumentNullException.ThrowIfNull(schedule.JobId);
        var schedulerSchedule = new SchedulerSchedule(schedule.ScheduleId, (Guid)schedule.JobId, schedule.CronExpression);
        await _schedulesManager.RemoveScheduleAsync(schedulerSchedule, CancellationToken.None);
    }

    public async Task DeleteJobAsync(Job job)
    {
        var schedulerJob = new SchedulerJob(job.JobId);
        await _schedulesManager.RemoveJobAsync(schedulerJob, CancellationToken.None);
    }

    public Task<(bool SchedulerDetected, bool SchedulerError)> GetStatusAsync() => Task.FromResult((true, DatabaseReadError));

    public async Task SynchronizeAsync()
    {
        try
        {
            await _schedulesManager.ReadAllSchedules(CancellationToken.None);
            DatabaseReadError = false;
        }
        catch (Exception)
        {
            DatabaseReadError = true;
            throw;
        }
    }

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(schedule.JobId);
        var schedulerSchedule = new SchedulerSchedule(schedule.ScheduleId, (Guid)schedule.JobId, schedule.CronExpression ?? string.Empty);
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
