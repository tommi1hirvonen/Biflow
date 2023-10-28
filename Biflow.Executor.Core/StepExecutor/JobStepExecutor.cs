using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class JobStepExecutor(
    ILogger<JobStepExecutor> logger,
    IExecutionManager executionManager,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IExecutionBuilderFactory<ExecutorDbContext> executionBuilderFactory,
    JobStepExecution step) : StepExecutorBase(logger, dbContextFactory, step)
{
    private readonly ILogger<JobStepExecutor> _logger = logger;
    private readonly IExecutionBuilderFactory<ExecutorDbContext> _executionBuilderFactory = executionBuilderFactory;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IExecutionManager _executionManager = executionManager;

    private JobStepExecution Step { get; } = step;

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var executionAttempt = Step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
        Guid jobExecutionId;
        try
        {
            var tagIds = Step.TagFilters switch
            {
                { Count: > 0 } tags => tags.Select(t => t.TagId).ToArray(),
                _ => null
            };
            var builder = await _executionBuilderFactory.CreateAsync(Step.JobToExecuteId, createdBy: null, parent: executionAttempt,
                context => step => step.IsEnabled,
                context => step => tagIds == null || step.Tags.Any(t => tagIds.Contains(t.TagId)));
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddAll();
            builder.Notify = Step.Execution.Notify;
            builder.NotifyCaller = Step.Execution.NotifyCaller;
            builder.NotifyCallerOvertime = Step.Execution.NotifyCallerOvertime;

            // Assign step parameter values to the initialized execution.
            if (Step.StepExecutionParameters.Any())
            {
                var parameters = Step.StepExecutionParameters
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
                AddWarning("Child job execution contained no steps");
                return Result.Success;
            }
            jobExecutionId = execution.ExecutionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error initializing execution for job {jobId}", Step.ExecutionId, Step, Step.JobToExecuteId);
            AddError(ex, "Error initializing job execution");
            return Result.Failure;
        }

        try
        {
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
            
        try
        {
            await _executionManager.StartExecutionAsync(jobExecutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {Step} Error starting executor process for execution {executionId}", Step.ExecutionId, Step, jobExecutionId);
            AddError(ex, "Error starting executor process");
            return Result.Failure;
        }

        if (Step.JobExecuteSynchronized)
        {
            try
            {
                await _executionManager.WaitForTaskCompleted(jobExecutionId, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _executionManager.CancelExecution(jobExecutionId, cancellationTokenSource.Username);
                AddWarning(ex);
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
                AddError(error);
                return Result.Failure;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ExecutionId} {Step} Error getting sub-execution status for execution id {executionId}", Step.ExecutionId, Step, jobExecutionId);
                AddError(ex, "Error getting sub-execution status");
                return Result.Failure;
            }
        }

        return Result.Success;
    }

}
