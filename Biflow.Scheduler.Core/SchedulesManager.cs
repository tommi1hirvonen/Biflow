using Biflow.Core.Entities;
using Biflow.Executor.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace Biflow.Scheduler.Core;

internal class SchedulesManager<TJob>(
    ILogger<SchedulesManager<TJob>> logger,
    IDbContextFactory<SchedulerDbContext> dbContextFactory,
    ISchedulerFactory schedulerFactory) : BackgroundService, ISchedulesManager
    where TJob : ExecutionJobBase
{
    private readonly ILogger _logger = logger;
    private readonly IScheduler _scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();
    private readonly IDbContextFactory<SchedulerDbContext> _dbContextFactory = dbContextFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool DatabaseReadError { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        do
        {
            try
            {
                await ReadAllSchedulesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading all schedules at startup");
            }
            await Task.Delay(TimeSpan.FromMinutes(15), cancellationToken);
        } while (DatabaseReadError) ;
    }

    public async Task<IEnumerable<JobStatus>> GetStatusAsync(CancellationToken cancellationToken)
    {
        var jobIds = await _scheduler.GetJobGroupNames(cancellationToken);
        var jobKeys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), cancellationToken);
        var triggerKeys = await _scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), cancellationToken);
        
        var runningSchedules = await _scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        var triggerStates = new Dictionary<TriggerKey, TriggerState>();
        foreach (var trigger in  triggerKeys)
        {
            var state = await _scheduler.GetTriggerState(trigger, cancellationToken);
            triggerStates[trigger] = state;
        }
        var jobDetails = new Dictionary<JobKey, IJobDetail>();
        foreach (var jobKey in jobKeys)
        {
            var detail = await _scheduler.GetJobDetail(jobKey, cancellationToken);
            if (detail is not null)
                jobDetails[jobKey] = detail;
        }
        var triggerDetails = new Dictionary<TriggerKey, ICronTrigger>();
        foreach (var triggerKey in  triggerKeys)
        {
            var detail = await _scheduler.GetTrigger(triggerKey, cancellationToken);
            if (detail is not null && detail is ICronTrigger cron)
                triggerDetails[triggerKey] = cron;
        }

        var jobStatuses = jobIds.Select(jobId =>
        {
            var statusSchedules = jobKeys
                .Where(key => key.Group == jobId) // Quartz job group maps to job id
                .Select(key =>
                {
                    var scheduleId = key.Name; // Quartz job name maps to schedule id
                    var trigger = triggerKeys.First(t => t.Name == scheduleId); // Trigger name maps to schedule id
                    var isEnabled = triggerStates.TryGetValue(trigger, out var state) && state != TriggerState.Paused;
                    var isRunning = runningSchedules.Any(r => r.JobDetail.Key == key);
                    var disallowConcurrentExecution = jobDetails.TryGetValue(key, out var detail) && detail.ConcurrentExecutionDisallowed;
                    var cronExpression = triggerDetails.GetValueOrDefault(trigger)?.CronExpressionString;
                    return new ScheduleStatus(scheduleId, cronExpression, isEnabled, isRunning, disallowConcurrentExecution);
                }).ToArray();
            return new JobStatus(jobId, statusSchedules);
        }).ToArray();

        return jobStatuses;
    }

    public async Task ReadAllSchedulesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            _logger.LogInformation("Loading schedules from database");

            List<Schedule> schedules;
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                schedules = await context.Schedules
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading schedules from database");
                throw;
            }

            // Clear the scheduler if there were any existing jobs or triggers.
            await _scheduler.Clear(cancellationToken);

            // Iterate the schedules and add them to the scheduler.
            var counter = 0;
            foreach (var schedule in schedules)
            {
                try
                {
                    await CreateAndAddScheduleAsync(SchedulerSchedule.From(schedule), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding schedule to internal scheduler");
                    throw;
                }
                counter++;
            }

            DatabaseReadError = false;

            _logger.LogInformation("{counter}/{Count} schedules loaded successfully", counter, schedules.Count);
        }
        catch
        {
            DatabaseReadError = true;
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
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
        var jobKey = new JobKey(schedule.ScheduleId.ToString(), schedule.JobId.ToString());
        if (!await _scheduler.DeleteJob(jobKey, cancellationToken))
        {
            throw new ScheduleNotFoundException(schedule);
        }
        _logger.LogInformation("Deleted schedule id {ScheduleId} for job id {JobId}", schedule.ScheduleId, schedule.JobId);
    }

    public async Task AddScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken)
    {
        await CreateAndAddScheduleAsync(schedule, cancellationToken);
    }

    public async Task UpdateScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken)
    {
        var jobKey = new JobKey(schedule.ScheduleId.ToString(), schedule.JobId.ToString());
        // Throw if previous schedule was not found. This could happen if the job of the schedule was changed.
        // Adding a new schedule without deleting the previous version could lead to undesired state of schedules.
        if (!await _scheduler.DeleteJob(jobKey, cancellationToken))
        {
            throw new ScheduleNotFoundException(schedule);
        }
        await CreateAndAddScheduleAsync(schedule, cancellationToken);
    }

    private async Task CreateAndAddScheduleAsync(SchedulerSchedule schedule, CancellationToken cancellationToken)
    {
        // Create one JobDetail per schedule.
        // Put schedules for the same job in the same group.
        var jobKey = new JobKey(schedule.ScheduleId.ToString(), schedule.JobId.ToString());
        var triggerKey = new TriggerKey(schedule.ScheduleId.ToString());
        var jobDetail = await _scheduler.GetJobDetail(jobKey, cancellationToken)
            ?? JobBuilder.Create<TJob>()
            .WithIdentity(jobKey)
            .DisallowConcurrentExecution(schedule.DisallowConcurrentExecution)
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

        _logger.LogInformation("Added schedule id {ScheduleId} for job id {JobId} with Cron expression {CronExpression}, status {status} and ",
                schedule.ScheduleId, schedule.JobId, schedule.CronExpression, schedule.IsEnabled);
    }
}