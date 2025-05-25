using Biflow.Core;
using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Biflow.Scheduler.Core;

public abstract class ExecutionJobBase(
    ILogger logger,
    [FromKeyedServices(SchedulerServiceKeys.JobStartFailuresHealthService)]
    HealthService healthService,
    IDbContextFactory<SchedulerDbContext> dbContextFactory,
    IExecutionBuilderFactory<SchedulerDbContext> executionBuilderFactory) : IJob
{
    private readonly ILogger _logger = logger;
    private readonly HealthService _healthService = healthService;
    private readonly IDbContextFactory<SchedulerDbContext> _dbContextFactory = dbContextFactory;
    private readonly IExecutionBuilderFactory<SchedulerDbContext> _executionBuilderFactory = executionBuilderFactory;

    protected abstract Task StartExecutorAsync(Guid executionId);

    protected abstract Task WaitForExecutionToFinish(Guid executionId);

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var jobKey = context.JobDetail.Key;
            var jobId = Guid.Parse(jobKey.Group);
            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var isEnabled = await dbContext.Jobs
                    .AsNoTracking()
                    .Where(job => job.JobId == jobId)
                    .Select(job => job.IsEnabled)
                    .FirstAsync();
                if (!isEnabled)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _healthService.AddError(context.JobDetail.Key.Group,
                    $"Error getting job IsEnabled status: {ex.Message}");
                _logger.LogError(ex, "Error getting job IsEnabled status");
                return;
            }

            var scheduleId = Guid.Parse(context.Trigger.Key.Name);
            Guid executionId;
            try
            {

                using var builder = await _executionBuilderFactory.CreateAsync(jobId, scheduleId,
                    [
                        _ => step => step.IsEnabled,
                        ctx => step =>
                            // Schedule has no tag filters
                            !ctx.Schedules.Any(sch => sch.ScheduleId == scheduleId && sch.TagFilter.Any()) ||
                            // There's at least one match between the step's tags and the schedule's tags
                            step.Tags.Any(t1 => ctx.Schedules.Any(sch => sch.ScheduleId == scheduleId && sch.TagFilter.Any(t2 => t1.TagId == t2.TagId)))
                    ]);
                ArgumentNullException.ThrowIfNull(builder);
                builder.AddAll();
                builder.Notify = true;
                var execution = await builder.SaveExecutionAsync();
                ArgumentNullException.ThrowIfNull(execution);
                executionId = execution.ExecutionId;
            }
            catch (Exception ex)
            {
                _healthService.AddError(jobId, $"Error initializing execution: {ex.Message}");
                _logger.LogError(ex, "Error initializing execution for job {jobId}", jobId);
                return;
            }

            try
            {
                await StartExecutorAsync(executionId);
            }
            catch (Exception ex)
            {
                _healthService.AddError(jobId, $"Error starting execution: {ex.Message}");
                _logger.LogError(ex, "Error starting execution for job {jobId}", jobId);
            }

            _logger.LogInformation("Started execution for job id {jobId}, schedule id {scheduleId}, execution id {executionId}", jobId, scheduleId, executionId);

            try
            {
                await WaitForExecutionToFinish(executionId);
            }
            catch (Exception ex)
            {
                _healthService.AddError(jobId, $"Error waiting for execution to finish: {ex.Message}");
                _logger.LogError(ex, "Error waiting for execution to finish for job {jobId}", jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quartz scheduled job threw an error");
        }
    }
}
