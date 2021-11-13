using Dapper;
using EtlManager.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Diagnostics;

namespace EtlManager.Scheduler;

[DisallowConcurrentExecution]
public class ExecutionJob : IJob
{
    private readonly ILogger<ExecutionJob> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

    public ExecutionJob(ILogger<ExecutionJob> logger, IConfiguration configuration, IDbContextFactory<EtlManagerContext> dbContextFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _dbContextFactory = dbContextFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var etlManagerConnectionString = _configuration.GetConnectionString("EtlManagerContext")
                ?? throw new ArgumentNullException("etlManagerConnectionString", "Connection string cannot be null");
            var executorFilePath = _configuration.GetValue<string>("EtlManagerExecutorPath")
                ?? throw new ArgumentNullException("executorFilePath", "Executor file path cannot be null");

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

            using var sqlConnection = new SqlConnection(etlManagerConnectionString);
            await sqlConnection.OpenAsync();

            Guid executionId;
            try
            {
                executionId = stepIds switch
                {
                    not null and { Count: > 0 } => await sqlConnection.ExecuteScalarAsync<Guid>(
                        "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_, @StepIds = @StepIds_, @ScheduleId = @ScheduleId_",
                        new { JobId_ = jobId, StepIds_ = string.Join(',', stepIds), ScheduleId_ = scheduleId }),
                    _ => await sqlConnection.ExecuteScalarAsync<Guid>(
                        "EXEC etlmanager.ExecutionInitialize @JobId = @JobId_, @ScheduleId = @ScheduleId_",
                        new { JobId_ = jobId, ScheduleId_ = scheduleId })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing execution for job {jobId}", jobId);
                return;
            }

            var executionInfo = new ProcessStartInfo()
            {
                FileName = executorFilePath,
                ArgumentList = {
                        "execute",
                        "--id",
                        executionId.ToString(),
                        "--notify"
                    },
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var executorProcess = new Process() { StartInfo = executionInfo };
            try
            {
                executorProcess.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting executor process for execution {executionId}", executionId);
                return;
            }

            _logger.LogInformation($"Started execution for job id {jobId}, schedule id {scheduleId}, execution id {executionId}");

            // Wait for the execution to finish and for the executor process to exit.
            // This way Quartz does not start a parallel execution of the same job.
            await executorProcess.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quartz scheduled job threw an error");
        }
    }
}
