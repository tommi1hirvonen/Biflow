using Dapper;
using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Biflow.Executor.Core.StepExecutor;

internal class JobStepExecutor : StepExecutorBase
{
    private readonly ILogger<JobStepExecutor> _logger;
    private readonly IExecutionConfiguration _executionConfiguration;
    private readonly IExecutorLauncher _executorLauncher;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    private JobStepExecution Step { get; }

    private StringBuilder Warning { get; } = new StringBuilder();

    public JobStepExecutor(
        ILogger<JobStepExecutor> logger,
        IExecutorLauncher executorLauncher,
        IDbContextFactory<BiflowContext> dbContextFactory,
        IExecutionConfiguration executionConfiguration,
        JobStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _executionConfiguration = executionConfiguration;
        _executorLauncher = executorLauncher;
        _dbContextFactory = dbContextFactory;
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        Guid jobExecutionId;
        try
        {
            using var connection = new SqlConnection(_executionConfiguration.ConnectionString);
            await connection.OpenAsync(CancellationToken.None);
            jobExecutionId = await connection.ExecuteScalarAsync<Guid>(
                "EXEC biflow.ExecutionInitialize @JobId = @JobId_, @Notify = @Notify_, @NotifyCaller = @NotifyCaller_, @NotifyCallerOvertime = @NotifyCallerOvertime_",
                new
                {
                    JobId_ = Step.JobToExecuteId,
                    Notify_ = Step.Execution.Notify,
                    NotifyCaller_ = Step.Execution.NotifyCaller,
                    NotifyCallerOvertime_ = Step.Execution.NotifyCallerOvertime
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error initializing execution for job {jobId}", Step.ExecutionId, Step, Step.JobToExecuteId);
            return Result.Failure($"Error initializing job execution:\n{ex.Message}", Warning.ToString());
        }

        try
        {
            var executionAttempt = Step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
            if (executionAttempt is JobStepExecutionAttempt job)
            {
                using var context = _dbContextFactory.CreateDbContext();
                job.ChildJobExecutionId = jobExecutionId;
                context.Attach(job);
                context.Entry(job).Property(p => p.ChildJobExecutionId).IsModified = true;
                await context.SaveChangesAsync(CancellationToken.None);
            }
            else
            {
                throw new InvalidOperationException("Could not find JobStepExecutionAttempt from StepExecution");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error logging child job execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
            Warning.AppendLine($"Error logging child job execution id\n{ex.Message}");
        }
            
        try
        {
            await _executorLauncher.StartExecutorAsync(jobExecutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting executor process for execution {executionId}", Step.ExecutionId, Step, jobExecutionId);
            return Result.Failure($"Error starting executor process:\n{ex.Message}", Warning.ToString());
        }

        if (Step.JobExecuteSynchronized)
        {
            try
            {
                await _executorLauncher.WaitForExitAsync(jobExecutionId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await _executorLauncher.CancelAsync(jobExecutionId, cancellationTokenSource.Username);
                throw;
            }

            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var status = await context.Executions
                    .Where(e => e.ExecutionId == jobExecutionId)
                    .Select(e => e.ExecutionStatus)
                    .FirstAsync();
                return status switch
                {
                    ExecutionStatus.Succeeded or ExecutionStatus.Warning => Result.Success(null, Warning.ToString()),
                    ExecutionStatus.Failed => Result.Failure("Sub-execution failed", Warning.ToString()),
                    ExecutionStatus.Stopped => Result.Failure("Sub-execution was stopped", Warning.ToString()),
                    ExecutionStatus.Suspended => Result.Failure("Sub-execution was suspended", Warning.ToString()),
                    ExecutionStatus.NotStarted => Result.Failure("Sub-execution failed to start", Warning.ToString()),
                    ExecutionStatus.Running => Result.Failure($"Sub-execution was finished but its status was reported as {status} after finishing", Warning.ToString()),
                    _ => Result.Failure("Unhandled sub-execution status", Warning.ToString()),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
                return Result.Failure("Error getting sub-execution status", Warning.ToString());
            }
        }

        return Result.Success(null, Warning.ToString());
    }

}
