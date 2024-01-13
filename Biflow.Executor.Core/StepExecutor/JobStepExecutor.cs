using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class JobStepExecutor(
    ILogger<JobStepExecutor> logger,
    IExecutionManager executionManager,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IExecutionBuilderFactory<ExecutorDbContext> executionBuilderFactory,
    JobStepExecution step) : IStepExecutor<JobStepExecutionAttempt>
{
    private readonly ILogger<JobStepExecutor> _logger = logger;
    private readonly IExecutionBuilderFactory<ExecutorDbContext> _executionBuilderFactory = executionBuilderFactory;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IExecutionManager _executionManager = executionManager;
    private readonly JobStepExecution _step = step;

    public JobStepExecutionAttempt Clone(JobStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(JobStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var executionAttempt = _step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
        Guid jobExecutionId;
        try
        {
            var tagIds = _step.TagFilters switch
            {
                { Count: > 0 } tags => tags.Select(t => t.TagId).ToArray(),
                _ => null
            };
            using var builder = await _executionBuilderFactory.CreateAsync(_step.JobToExecuteId, createdBy: null, parent: executionAttempt,
                context => step => step.IsEnabled,
                context => step => tagIds == null || step.Tags.Any(t => tagIds.Contains(t.TagId)));
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddAll();
            builder.Notify = _step.Execution.Notify;
            builder.NotifyCaller = _step.Execution.NotifyCaller;
            builder.NotifyCallerOvertime = _step.Execution.NotifyCallerOvertime;

            // Assign step parameter values to the initialized execution.
            if (_step.StepExecutionParameters.Any())
            {
                var parameters = _step.StepExecutionParameters
                    .Cast<JobStepExecutionParameter>()
                    .Join(builder.Parameters,
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
            }
            
            var execution = await builder.SaveExecutionAsync();
            if (execution is null)
            {
                attempt.AddWarning("Child job execution contained no steps");
                return Result.Success;
            }
            jobExecutionId = execution.ExecutionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error initializing execution for job {jobId}", _step.ExecutionId, _step, _step.JobToExecuteId);
            attempt.AddError(ex, "Error initializing job execution");
            return Result.Failure;
        }

        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            attempt.ChildJobExecutionId = jobExecutionId;
            context.Attach(attempt);
            context.Entry(attempt).Property(p => p.ChildJobExecutionId).IsModified = true;
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error logging child job execution id {executionId}", _step.ExecutionId, _step, jobExecutionId);
            attempt.AddWarning(ex, $"Error logging child job execution id {jobExecutionId}");
        }
            
        try
        {
            await _executionManager.StartExecutionAsync(jobExecutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting executor process for execution {executionId}", _step.ExecutionId, _step, jobExecutionId);
            attempt.AddError(ex, "Error starting executor process");
            return Result.Failure;
        }

        if (_step.JobExecuteSynchronized)
        {
            try
            {
                await _executionManager.WaitForTaskCompleted(jobExecutionId, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _executionManager.CancelExecution(jobExecutionId, cancellationTokenSource.Username);
                attempt.AddWarning(ex);
                return Result.Cancel;
            }

            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var status = await context.Executions
                    .Where(e => e.ExecutionId == jobExecutionId)
                    .Select(e => e.ExecutionStatus)
                    .FirstAsync();
                
                if (status is ExecutionStatus.Succeeded or ExecutionStatus.Warning)
                {
                    return Result.Success;
                }

                var error = status switch
                {
                    ExecutionStatus.Failed => "Sub-execution failed",
                    ExecutionStatus.Stopped => "Sub-execution was stopped",
                    ExecutionStatus.Suspended => "Sub-execution was suspended",
                    ExecutionStatus.NotStarted => "Sub-execution failed to start",
                    ExecutionStatus.Running => $"Sub-execution was finished but its status was reported as {status} after finishing",
                    _ => "Unhandled sub-execution status",
                };
                attempt.AddError(error);
                return Result.Failure;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", _step.ExecutionId, _step, jobExecutionId);
                attempt.AddError(ex, "Error getting sub-execution status");
                return Result.Failure;
            }
        }

        return Result.Success;
    }

}
