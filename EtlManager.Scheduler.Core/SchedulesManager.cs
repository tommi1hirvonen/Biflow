using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EtlManager.Scheduler.Core;

internal class SchedulesManager<TJob> : ISchedulesManager where TJob : ExecutionJobBase
{
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

    public SchedulesManager(ILogger<SchedulesManager<TJob>> logger, IDbContextFactory<EtlManagerContext> dbContextFactory, ISchedulerFactory schedulerFactory)
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
            if (schedule.CronExpression is null)
                throw new ArgumentNullException(nameof(schedule.CronExpression), "Cron expression cannot be null");

            var jobKey = new JobKey(schedule.JobId.ToString());
            var triggerKey = new TriggerKey(schedule.ScheduleId.ToString());
            var jobDetail = JobBuilder.Create<TJob>()
                .WithIdentity(jobKey)
                .Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .ForJob(jobDetail)
                .WithCronSchedule(schedule.CronExpression, x => x.WithMisfireHandlingInstructionDoNothing())
                .Build();

            if (!await _scheduler.CheckExists(jobKey, cancellationToken))
            {
                await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
            }
            else
            {
                await _scheduler.ScheduleJob(trigger, cancellationToken);
            }

            if (!schedule.IsEnabled)
            {
                await _scheduler.PauseTrigger(triggerKey, cancellationToken);
            }

            var status = schedule.IsEnabled == true ? "Enabled" : "Paused";
            _logger.LogInformation($"Added schedule id {schedule.ScheduleId} for job id {schedule.JobId} with Cron expression {schedule.CronExpression} and status {status}");

            counter++;
        }

        _logger.LogInformation($"{counter}/{schedules.Count} schedules loaded successfully");
    }

    public async Task ResumeScheduleAsync(Schedule schedule, CancellationToken cancellationToken)
    {

        var triggerKey = new TriggerKey(schedule.ScheduleId.ToString());
        await _scheduler.ResumeTrigger(triggerKey, cancellationToken);

        _logger.LogInformation($"Resumed schedule id {schedule.ScheduleId} for job id {schedule.JobId}");
    }

    public async Task PauseScheduleAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        var triggerKey = new TriggerKey(schedule.ScheduleId.ToString());
        await _scheduler.PauseTrigger(triggerKey, cancellationToken);

        _logger.LogInformation($"Paused schedule id {schedule.ScheduleId} for job id {schedule.JobId}");
    }

    public async Task RemoveJobAsync(Job job, CancellationToken cancellationToken)
    {
        var jobKey = new JobKey(job.JobId.ToString());
        await _scheduler.DeleteJob(jobKey, cancellationToken);

        _logger.LogInformation($"Deleted all schedules for job id {job.JobId}");
    }

    public async Task RemoveScheduleAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        var triggerKey = new TriggerKey(schedule.ScheduleId.ToString());
        await _scheduler.UnscheduleJob(triggerKey, cancellationToken);

        _logger.LogInformation($"Deleted schedule id {schedule.ScheduleId} for job id {schedule.JobId}");
    }

    public async Task AddScheduleAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schedule.CronExpression);

        // Check that the Cron expression is valid.
        if (!CronExpression.IsValidExpression(schedule.CronExpression))
            throw new ArgumentException($"Invalid Cron expression for schedule id {schedule.ScheduleId}: {schedule.CronExpression}");

        var jobKey = new JobKey(schedule.JobId.ToString());
        var jobDetail = await _scheduler.GetJobDetail(jobKey, cancellationToken)
            ?? JobBuilder.Create<TJob>()
            .WithIdentity(schedule.JobId.ToString())
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity(schedule.ScheduleId.ToString())
            .ForJob(jobDetail)
            .WithCronSchedule(schedule.CronExpression, x => x.WithMisfireHandlingInstructionDoNothing())
            .Build();

        if (!await _scheduler.CheckExists(jobKey, cancellationToken))
        {
            await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
        }
        else
        {
            await _scheduler.ScheduleJob(trigger, cancellationToken);
        }

        _logger.LogInformation($"Added schedule id {schedule.ScheduleId} for job id {schedule.JobId} with Cron expression {schedule.CronExpression}");
    }

}
