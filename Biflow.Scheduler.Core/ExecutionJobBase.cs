using Biflow.Core;
using Biflow.DataAccess;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Biflow.Scheduler.Core;

public abstract class ExecutionJobBase : IJob
{
    private readonly ILogger _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    protected abstract Task StartExecutorAsync(Guid executionId);

    protected abstract Task WaitForExecutionToFinish(Guid executionId);

    public ExecutionJobBase(ILogger logger, IDbContextFactory<BiflowContext> dbContextFactory, ISqlConnectionFactory sqlConnectionFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _sqlConnectionFactory = sqlConnectionFactory;
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

            Guid executionId;
            try
            {
                using var sqlConnection = _sqlConnectionFactory.Create();
                await sqlConnection.OpenAsync();
                executionId = await sqlConnection.ExecuteScalarAsync<Guid>(
                    "EXEC biflow.ExecutionInitialize @JobId = @JobId_, @ScheduleId = @ScheduleId_, @Notify = @Notify_",
                    new { JobId_ = jobId, ScheduleId_ = scheduleId, Notify_ = true });
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
