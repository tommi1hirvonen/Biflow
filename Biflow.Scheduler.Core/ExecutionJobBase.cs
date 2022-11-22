using Dapper;
using Biflow.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Biflow.Scheduler.Core;

[DisallowConcurrentExecution]
public abstract class ExecutionJobBase : IJob
{
    private readonly ILogger _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    protected abstract string BiflowConnectionString { get; }

    protected abstract Task StartExecutorAsync(Guid executionId);

    protected abstract Task WaitForExecutionToFinish(Guid executionId);

    public ExecutionJobBase(ILogger logger, IDbContextFactory<BiflowContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var jobId = Guid.Parse(context.JobDetail.Key.Name);
            var scheduleId = Guid.Parse(context.Trigger.Key.Name);
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var isEnabled = await dbContext.Jobs
                    .AsNoTracking()
                    .Where(job => job.JobId == jobId)
                    .Select(job => job.IsEnabled)
                    .FirstAsync();
                if (!isEnabled)
                    return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job IsEnabled status");
                return;
            }

            // Create a list of step ids based on the schedule's tag filters.
            // The list will be null if no tag filters were applied.
            List<Guid>? stepIds = null;
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                var scheduleTags = await dbContext.Schedules
                    .AsNoTrackingWithIdentityResolution()
                    .Include(s => s.Tags)
                    .Where(s => s.ScheduleId == scheduleId)
                    .SelectMany(s => s.Tags.Select(t => t.TagId))
                    .ToListAsync();
                if (scheduleTags.Any())
                {
                    stepIds = await dbContext.Steps
                        .AsNoTrackingWithIdentityResolution()
                        .Include(s => s.Tags)
                        .Where(s => s.JobId == jobId)
                        .Where(s => s.Tags.Any(t => scheduleTags.Contains(t.TagId)))
                        .Select(s => s.StepId)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting step ids based on schedule's tag filters");
                return;
            }

            using var sqlConnection = new SqlConnection(BiflowConnectionString);
            await sqlConnection.OpenAsync();

            Guid executionId;
            try
            {
                executionId = stepIds switch
                {
                    not null and { Count: > 0 } => await sqlConnection.ExecuteScalarAsync<Guid>(
                        "EXEC biflow.ExecutionInitialize @JobId = @JobId_, @StepIds = @StepIds_, @ScheduleId = @ScheduleId_, @Notify = @Notify_",
                        new { JobId_ = jobId, StepIds_ = string.Join(',', stepIds), ScheduleId_ = scheduleId, Notify_ = true }),
                    _ => await sqlConnection.ExecuteScalarAsync<Guid>(
                        "EXEC biflow.ExecutionInitialize @JobId = @JobId_, @ScheduleId = @ScheduleId_, @Notify = @Notify_",
                        new { JobId_ = jobId, ScheduleId_ = scheduleId, Notify_ = true })
                };
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
