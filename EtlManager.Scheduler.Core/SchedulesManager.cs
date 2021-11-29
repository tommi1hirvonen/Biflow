using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EtlManager.Scheduler.Core;

public class SchedulesManager<TJob> where TJob : ExecutionJobBase
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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding schedule object {schedule}");
            }
        }

        _logger.LogInformation($"{counter}/{schedules.Count} schedules loaded successfully");
    }

    public async Task ResumeScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken)
    {
        if (command.ScheduleId is null)
            throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

        var triggerKey = new TriggerKey(command.ScheduleId);
        await _scheduler.ResumeTrigger(triggerKey, cancellationToken);

        _logger.LogInformation($"Resumed schedule id {command.ScheduleId} for job id {command.JobId}");
    }

    public async Task PauseScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken)
    {
        if (command.ScheduleId is null)
            throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

        var triggerKey = new TriggerKey(command.ScheduleId);
        await _scheduler.PauseTrigger(triggerKey, cancellationToken);

        _logger.LogInformation($"Paused schedule id {command.ScheduleId} for job id {command.JobId}");
    }

    public async Task RemoveScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken)
    {
        // If no schedule was mentioned, delete all schedules for the given job.
        if (command.ScheduleId is null)
        {
            if (command.JobId is null)
                throw new ArgumentNullException(nameof(command.JobId), "Schedule id and job id were null when one of them should be given");

            var jobKey = new JobKey(command.JobId);
            await _scheduler.DeleteJob(jobKey, cancellationToken);

            _logger.LogInformation($"Deleted all schedules for job id {command.JobId}");
        }
        // Otherwise delete only the given schedule.
        else
        {
            var triggerKey = new TriggerKey(command.ScheduleId);
            await _scheduler.UnscheduleJob(triggerKey, cancellationToken);

            _logger.LogInformation($"Deleted schedule id {command.ScheduleId} for job id {command.JobId}");
        }
    }

    public async Task AddScheduleAsync(SchedulerCommand command, CancellationToken cancellationToken)
    {
        if (command.CronExpression is null)
            throw new ArgumentNullException(nameof(command.CronExpression), "Cron expression was null");

        if (command.ScheduleId is null)
            throw new ArgumentNullException(nameof(command.ScheduleId), "Schedule id was null");

        if (command.JobId is null)
            throw new ArgumentNullException(nameof(command.JobId), "Job id was null");

        // Check that the Cron expression is valid.
        if (!CronExpression.IsValidExpression(command.CronExpression))
            throw new ArgumentException($"Invalid Cron expression for schedule id {command.ScheduleId}: {command.CronExpression}");

        var jobKey = new JobKey(command.JobId);
        var jobDetail = await _scheduler.GetJobDetail(jobKey, cancellationToken)
            ?? JobBuilder.Create<TJob>()
            .WithIdentity(command.JobId)
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity(command.ScheduleId)
            .ForJob(jobDetail)
            .WithCronSchedule(command.CronExpression, x => x.WithMisfireHandlingInstructionDoNothing())
            .Build();

        if (!await _scheduler.CheckExists(jobKey, cancellationToken))
        {
            await _scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
        }
        else
        {
            await _scheduler.ScheduleJob(trigger, cancellationToken);
        }

        _logger.LogInformation($"Added schedule id {command.ScheduleId} for job id {command.JobId} with Cron expression {command.CronExpression}");
    }

}
