﻿using Biflow.Core;
using Biflow.Scheduler.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Biflow.Ui.Core;

public class SelfHostedSchedulerService(
    ISchedulesManager schedulesManager,
    [FromKeyedServices(SchedulerServiceKeys.JobStartFailuresHealthService)]
    HealthService jobStartFailuresHealthService,
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

    public async Task<IEnumerable<JobStatus>> GetStatusAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var jobStatuses = await schedulesManager.GetStatusAsync(cts.Token);
        return jobStatuses;
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
        // When running scheduler in self-hosted mode, only run health checks for the scheduler service
        // (tag == "scheduler").
        var healthReport = await healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("scheduler"),
            cancellationToken);
        return new HealthReportDto(healthReport);
    }
    
    public Task ClearTransientHealthErrorsAsync(CancellationToken cancellationToken = default)
    {
        jobStartFailuresHealthService.ClearErrors();
        return Task.CompletedTask;
    }
}
