using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class JobStepExecutor(
    ILogger<JobStepExecutor> logger,
    IExecutionManager executionManager,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IExecutionBuilderFactory<ExecutorDbContext> executionBuilderFactory,
    JobStepExecution step,
    JobStepExecutionAttempt attempt) : IStepExecutor
{
    public async Task<Result> ExecuteAsync(OrchestrationContext context, ExtendedCancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var executionAttempt = step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
        Guid jobExecutionId;
        try
        {
            var tagIds = step.TagFilters.Any() switch
            {
                true => step.TagFilters.Select(t => t.TagId).ToArray(),
                _ => null
            };
            using var builder = await executionBuilderFactory.CreateAsync(
                step.JobToExecuteId,
                createdBy: null,
                parent: executionAttempt,
                predicates:
                [
                    _ => s => s.IsEnabled,
                    _ => s => tagIds == null || s.Tags.Any(t => tagIds.Contains(t.TagId))
                ],
                CancellationToken.None);
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddAll();
            builder.Notify = step.Execution.Notify;
            builder.NotifyCaller = step.Execution.NotifyCaller;
            builder.NotifyCallerOvertime = step.Execution.NotifyCallerOvertime;

            // If the step has timeout defined, overwrite the execution timeout setting.
            if (step.TimeoutMinutes > 0)
            {
                builder.TimeoutMinutes = step.TimeoutMinutes;
            }

            // Assign step parameter values to the initialized execution.
            if (step.StepExecutionParameters.Any())
            {
                var parameters = step.StepExecutionParameters
                    .Join(builder.Parameters,
                    stepParam => stepParam.AssignToJobParameterId,
                    jobParam => jobParam.ParameterId,
                    (stepParam, jobParam) => (stepParam, jobParam));
                foreach (var (stepParam, jobParam) in parameters)
                {
                    jobParam.ParameterValue = stepParam.ParameterValue;
                    // Override UseExpression since the parameter is set with a value that may have been evaluated in this execution.
                    jobParam.UseExpression = false;
                }
            }
            
            var execution = await builder.SaveExecutionAsync(CancellationToken.None);
            if (execution is null)
            {
                attempt.AddWarning("Child job execution contained no steps");
                return Result.Success;
            }
            jobExecutionId = execution.ExecutionId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error initializing execution for job {jobId}",
                step.ExecutionId, step, step.JobToExecuteId);
            attempt.AddError(ex, "Error initializing job execution");
            return Result.Failure;
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            attempt.ChildJobExecutionId = jobExecutionId;
            await dbContext.Set<JobStepExecutionAttempt>()
                .Where(x => x.ExecutionId == attempt.ExecutionId &&
                            x.StepId == attempt.StepId &&
                            x.RetryAttemptIndex == attempt.RetryAttemptIndex)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.ChildJobExecutionId, attempt.ChildJobExecutionId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error logging child job execution id {jobExecutionId}",
                step.ExecutionId, step, jobExecutionId);
            attempt.AddWarning(ex, $"Error logging child job execution id {jobExecutionId}");
        }
            
        try
        {
            // Create a new orchestration context. Use the previous parent execution id if available.
            var nextContext = new OrchestrationContext(
                executionId: jobExecutionId,
                parentExecutionId: context.ParentExecutionId ?? step.ExecutionId,
                synchronizedExecution: step.JobExecuteSynchronized);
            await executionManager.StartExecutionAsync(nextContext, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error starting executor process for execution {jobExecutionId}",
                step.ExecutionId, step, jobExecutionId);
            attempt.AddError(ex, "Error starting executor process");
            return Result.Failure;
        }

        if (!step.JobExecuteSynchronized)
        {
            return Result.Success;
        }
        
        try
        {
            await executionManager.WaitForTaskCompleted(jobExecutionId, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            executionManager.CancelExecution(jobExecutionId, cts.Username);
            attempt.AddWarning(ex);
            return Result.Cancel;
        }

        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
            var status = await dbContext.Executions
                .Where(e => e.ExecutionId == jobExecutionId)
                .Select(e => e.ExecutionStatus)
                .FirstAsync(CancellationToken.None);
            
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
                _ => "Unhandled sub-execution status"
            };
            attempt.AddError(error);
            return Result.Failure;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {jobExecutionId}",
                step.ExecutionId, step, jobExecutionId);
            attempt.AddError(ex, "Error getting sub-execution status");
            return Result.Failure;
        }
    }

    public void Dispose()
    {
    }
}
