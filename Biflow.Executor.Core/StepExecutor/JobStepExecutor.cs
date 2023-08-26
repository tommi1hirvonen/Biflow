using Biflow.Core;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class JobStepExecutor : StepExecutorBase
{
    private readonly ILogger<JobStepExecutor> _logger;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly IExecutorLauncher _executorLauncher;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;

    private JobStepExecution Step { get; }

    public JobStepExecutor(
        ILogger<JobStepExecutor> logger,
        IExecutorLauncher executorLauncher,
        IDbContextFactory<ExecutorDbContext> dbContextFactory,
        ISqlConnectionFactory sqlConnectionFactory,
        JobStepExecution step)
        : base(logger, dbContextFactory, step)
    {
        _logger = logger;
        _sqlConnectionFactory = sqlConnectionFactory;
        _executorLauncher = executorLauncher;
        _dbContextFactory = dbContextFactory;
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        string? stepIds = null;
        if (Step.TagFilters.Any())
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var steps = await context.Steps
                    .Where(step => step.JobId == Step.JobToExecuteId)
                    .Where(step => step.Tags.Any(tag => Step.TagFilters.Select(t => t.TagId).Contains(tag.TagId)))
                    .Select(step => step.StepId)
                    .ToListAsync();
                if (steps.Any())
                {
                    stepIds = string.Join(",", steps);
                }
                else
                {
                    AddWarning("No steps to include in execution with current tag filters");
                    return new Success();
                }
            }
            catch (Exception ex)
            {
                return new Failure(ex, "Error getting list of steps based on tag filters");
            }
        }

        Guid jobExecutionId;
        try
        {
            using var connection = _sqlConnectionFactory.Create();
            await connection.OpenAsync(CancellationToken.None);
            jobExecutionId = await connection.ExecuteScalarAsync<Guid>(
                "EXEC biflow.ExecutionInitialize @JobId = @JobId_, @StepIds = @StepIds_, @Notify = @Notify_, @NotifyCaller = @NotifyCaller_, @NotifyCallerOvertime = @NotifyCallerOvertime_",
                new
                {
                    JobId_ = Step.JobToExecuteId,
                    StepIds_ = stepIds,
                    Notify_ = Step.Execution.Notify,
                    NotifyCaller_ = Step.Execution.NotifyCaller,
                    NotifyCallerOvertime_ = Step.Execution.NotifyCallerOvertime
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error initializing execution for job {jobId}", Step.ExecutionId, Step, Step.JobToExecuteId);
            return new Failure(ex, "Error initializing job execution");
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
                    // Override UseExpression since the parameter is set with a value that may have been evaluated in this execution.
                    jobParam.UseExpression = false;
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new Failure(ex, $"Error assigning step parameter values to initialized execution's parameters for execution id {jobExecutionId}");
            }
        }
            
        try
        {
            await _executorLauncher.StartExecutorAsync(jobExecutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting executor process for execution {executionId}", Step.ExecutionId, Step, jobExecutionId);
            return new Failure(ex, "Error starting executor process");
        }

        if (Step.JobExecuteSynchronized)
        {
            try
            {
                await _executorLauncher.WaitForExitAsync(jobExecutionId, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                await _executorLauncher.CancelAsync(jobExecutionId, cancellationTokenSource.Username);
                return new Cancel(ex);
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
                    ExecutionStatus.Succeeded or ExecutionStatus.Warning => new Success(),
                    ExecutionStatus.Failed => new Failure("Sub-execution failed"),
                    ExecutionStatus.Stopped => new Failure("Sub-execution was stopped"),
                    ExecutionStatus.Suspended => new Failure("Sub-execution was suspended"),
                    ExecutionStatus.NotStarted => new Failure("Sub-execution failed to start"),
                    ExecutionStatus.Running => new Failure($"Sub-execution was finished but its status was reported as {status} after finishing"),
                    _ => new Failure("Unhandled sub-execution status"),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
                return new Failure(ex, "Error getting sub-execution status");
            }
        }

        return new Success();
    }

}
