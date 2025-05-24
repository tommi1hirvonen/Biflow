using Biflow.Scheduler.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Biflow.Ui.Core;

public class SelfHostedSchedulerService(
    ISchedulesManager schedulesManager,
    HealthCheckService healthCheckService) : ISchedulerService
{
    public async Task AddScheduleAsync(Schedule schedule)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        await schedulesManager.AddScheduleAsync(schedulerSchedule, CancellationToken.None);
    }

    public async Task RemoveScheduleAsync(Schedule schedule)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        await schedulesManager.RemoveScheduleAsync(schedulerSchedule, CancellationToken.None);
    }

    public async Task UpdateScheduleAsync(Schedule schedule)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        await schedulesManager.UpdateScheduleAsync(schedulerSchedule, CancellationToken.None);
    }

    public async Task DeleteJobAsync(Guid jobId)
    {
        var schedulerJob = new SchedulerJob(jobId);
        await schedulesManager.RemoveJobAsync(schedulerJob, CancellationToken.None);
    }

    public async Task<SchedulerStatusResponse> GetStatusAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        SchedulerStatusResponse response = schedulesManager.DatabaseReadException is not null
            ? new SchedulerError()
            : new Success(await schedulesManager.GetStatusAsync(cts.Token));
        return response;
    }

    public Task SynchronizeAsync() => schedulesManager.ReadAllSchedulesAsync(CancellationToken.None);

    public async Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled)
    {
        var schedulerSchedule = SchedulerSchedule.From(schedule);
        if (enabled)
        {
            await schedulesManager.ResumeScheduleAsync(schedulerSchedule, CancellationToken.None);
        }
        else
        {
            await schedulesManager.PauseScheduleAsync(schedulerSchedule, CancellationToken.None);
        }
    }

    public async Task<HealthReportDto> GetHealthReportAsync(CancellationToken cancellationToken = default)
    {
        var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);
        return new HealthReportDto(healthReport);
    }
}
