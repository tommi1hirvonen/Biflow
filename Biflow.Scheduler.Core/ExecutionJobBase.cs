using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Biflow.Scheduler.Core;

public abstract class ExecutionJobBase : IJob
{
    private readonly ILogger _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    private readonly IExecutionBuilderFactory _executionBuilderFactory;

    protected abstract Task StartExecutorAsync(Guid executionId);

    protected abstract Task WaitForExecutionToFinish(Guid executionId);

    public ExecutionJobBase(ILogger logger, IDbContextFactory<BiflowContext> dbContextFactory, IExecutionBuilderFactory executionBuilderFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _executionBuilderFactory = executionBuilderFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var jobId = Guid.Parse(context.JobDetail.Key.Name);
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
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
                _logger.LogError(ex, "Error getting job IsEnabled status");
                return;
            }

            var scheduleId = Guid.Parse(context.Trigger.Key.Name);
            Guid executionId;
            try
            {

                var builder = await _executionBuilderFactory.CreateAsync(jobId, scheduleId,
                    context => step => step.IsEnabled,
                    context => step =>
                    // Schedule has no tag filters
                    !context.Schedules.Any(sch => sch.ScheduleId == scheduleId && sch.Tags.Any()) ||
                    // There's at least one match between the step's tags and the schedule's tags
                    step.Tags.Any(t1 => context.Schedules.Any(sch => sch.ScheduleId == scheduleId && sch.Tags.Any(t2 => t1.TagId == t2.TagId))));
                ArgumentNullException.ThrowIfNull(builder);
                builder.AddAll();
                builder.Notify = true;
                var execution = await builder.SaveExecutionAsync();
                ArgumentNullException.ThrowIfNull(execution);
                executionId = execution.ExecutionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing execution for job {jobId}", jobId);
                return;
            }

            await StartExecutorAsync(executionId);

            _logger.LogInformation("Started execution for job id {jobId}, schedule id {scheduleId}, execution id {executionId}", jobId, scheduleId, executionId);

            await WaitForExecutionToFinish(executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quartz scheduled job threw an error");
        }
    }
}
