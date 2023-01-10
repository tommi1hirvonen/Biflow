using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class JobStepExecutor : StepExecutorBase
{
    private readonly ILogger<JobStepExecutor> _logger;
    private readonly IExecutionConfiguration _executionConfiguration;
    private readonly IExecutorLauncher _executorLauncher;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    private JobStepExecution Step { get; }

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
            return Result.Failure(ex, "Error initializing job execution");
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
            AddWarning(ex, $"Error logging child job execution id {jobExecutionId}");
        }

        // Assign step parameter values to the initialized execution.
        if (Step.StepExecutionParameters.Any())
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var execution = await context.Executions
                    .Include(e => e.ExecutionParameters)
                    .FirstAsync(e => e.ExecutionId == jobExecutionId);
                var parameters = Step.StepExecutionParameters
                    .Cast<JobStepExecutionParameter>()
                    .Join(execution.ExecutionParameters,
                    stepParam => stepParam.AssignToJobParameterId,
                    jobParam => jobParam.ParameterId,
                    (stepParam, jobParam) => (stepParam, jobParam));
                foreach (var (stepParam, jobParam) in parameters)
                {
                    jobParam.ParameterValueType = stepParam.ParameterValueType;
                    jobParam.ParameterValue = stepParam.ParameterValue;
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex, $"Error assigning step parameter values to initialized execution's parameters for execution id {jobExecutionId}");
            }
        }
            
        try
        {
            await _executorLauncher.StartExecutorAsync(jobExecutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting executor process for execution {executionId}", Step.ExecutionId, Step, jobExecutionId);
            return Result.Failure(ex, "Error starting executor process");
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
                    ExecutionStatus.Succeeded or ExecutionStatus.Warning => Result.Success(),
                    ExecutionStatus.Failed => Result.Failure("Sub-execution failed"),
                    ExecutionStatus.Stopped => Result.Failure("Sub-execution was stopped"),
                    ExecutionStatus.Suspended => Result.Failure("Sub-execution was suspended"),
                    ExecutionStatus.NotStarted => Result.Failure("Sub-execution failed to start"),
                    ExecutionStatus.Running => Result.Failure($"Sub-execution was finished but its status was reported as {status} after finishing"),
                    _ => Result.Failure("Unhandled sub-execution status"),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
                return Result.Failure(ex, "Error getting sub-execution status");
            }
        }

        return Result.Success();
    }

}
