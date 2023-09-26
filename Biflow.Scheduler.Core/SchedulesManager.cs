using Biflow.DataAccess.Models;
using Biflow.Executor.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace Biflow.Scheduler.Core;

internal class SchedulesManager<TJob> : ISchedulesManager where TJob : ExecutionJobBase
{
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;
    private readonly IDbContextFactory<SchedulerDbContext> _dbContextFactory;

    public SchedulesManager(ILogger<SchedulesManager<TJob>> logger, IDbContextFactory<SchedulerDbContext> dbContextFactory, ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _scheduler = schedulerFactory.GetScheduler().Result;
    }

    public async Task ReadAllSchedules(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading schedules from database");

        List<Schedule> schedules;
        using (var context = _dbContextFactory.CreateDbContext())
        {
            schedules = await context.Schedules
                .AsNoTracking()
                .ToListAsync(cancellationToken: cancellationToken);
        }

        // Clear the scheduler if there were any existing jobs or triggers.
        await _scheduler.Clear(cancellationToken);

        // Iterate the schedules and add them to the scheduler.
        var counter = 0;
        foreach (var schedule in schedules)
        {
            ArgumentNullException.ThrowIfNull(schedule.CronExpression);
            await CreateAndAddScheduleAsync(schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.DisallowConcurrentExecution, schedule.IsEnabled, cancellationToken);
            
            var status = schedule.IsEnabled == true ? "Enabled" : "Paused";
            _logger.LogInformation("Added schedule id {ScheduleId} for job id {JobId} with Cron expression {CronExpression} and status {status}",
                schedule.ScheduleId, schedule.JobId, schedule.CronExpression, status);

            counter++;
        }

        _logger.LogInformation("{counter}/{Count} schedules loaded successfully", counter, schedules.Count);
    }

    public async Task ResumeScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken)
    {

        var triggerKey = new TriggerKey(schedule.ScheduleId.ToString());
        await _scheduler.ResumeTrigger(triggerKey, cancellationToken);

        _logger.LogInformation("Resumed schedule id {ScheduleId} for job id {JobId}", schedule.ScheduleId, schedule.JobId);
    }

    public async Task PauseScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken)
    {
        var triggerKey = new TriggerKey(schedule.ScheduleId.ToString());
        await _scheduler.PauseTrigger(triggerKey, cancellationToken);

        _logger.LogInformation("Paused schedule id {ScheduleId} for job id {JobId}", schedule.ScheduleId, schedule.JobId);
    }

    public async Task RemoveJobAsync(SchedulerJob job, CancellationToken cancellationToken)
    {
        var matcher = GroupMatcher<JobKey>.GroupEquals(job.JobId.ToString());
        var jobKeys = await _scheduler.GetJobKeys(matcher, cancellationToken);
        await _scheduler.DeleteJobs(jobKeys, cancellationToken);

        _logger.LogInformation("Deleted all schedules for job id {JobId}", job.JobId);
    }

    public async Task RemoveScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken)
    {
        var triggerKey = new TriggerKey(schedule.ScheduleId.ToString());
        await _scheduler.UnscheduleJob(triggerKey, cancellationToken);
        
        var jobKey = new JobKey(schedule.ScheduleId.ToString(), schedule.JobId.ToString());
        await _scheduler.DeleteJob(jobKey, cancellationToken);

        _logger.LogInformation("Deleted schedule id {ScheduleId} for job id {JobId}", schedule.ScheduleId, schedule.JobId);
    }

    public async Task AddScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken)
    {
        await CreateAndAddScheduleAsync(schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.DisallowConcurrentExecution, true, cancellationToken);

        _logger.LogInformation("Added schedule id {ScheduleId} for job id {JobId} with Cron expression {CronExpression}",
            schedule.ScheduleId, schedule.JobId, schedule.CronExpression);
    }

    private async Task CreateAndAddScheduleAsync(Guid scheduleId, Guid jobId, string cronExpression, bool disallowConcurrentExecution, bool isEnabled, CancellationToken cancellationToken)
    {
        // Create one JobDetail per schedule.
        // Put schedules for the same job in the same group.
        var jobKey = new JobKey(scheduleId.ToString(), jobId.ToString());
        var triggerKey = new TriggerKey(scheduleId.ToString());
        var jobDetail = await _scheduler.GetJobDetail(jobKey, cancellationToken)
            ?? JobBuilder.Create<TJob>()
            .WithIdentity(jobKey)
            .DisallowConcurrentExecution(disallowConcurrentExecution)
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobDetail)
            .WithCronSchedule(cronExpression, x => x.WithMisfireHandlingInstructionDoNothing())
            .Build();

        if (!await _scheduler.CheckExists(jobKey, cancellationToken))
        {
            await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
        }
        else
        {
            await _scheduler.ScheduleJob(trigger, cancellationToken);
        }

        if (!isEnabled)
        {
            await _scheduler.PauseTrigger(triggerKey, cancellationToken);
        }
    }

}
