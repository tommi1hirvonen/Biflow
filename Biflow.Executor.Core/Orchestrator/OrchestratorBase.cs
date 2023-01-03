using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.PowerBI.Api.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal abstract class OrchestratorBase
{
    private readonly ILogger<OrchestratorBase> _logger;
    private readonly IExecutionConfiguration _executionConfig;
    private readonly IStepExecutorFactory _stepExecutorFactory;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    private readonly SemaphoreSlim _mainSemaphore;
    private readonly Dictionary<StepType, SemaphoreSlim> _stepTypeSemaphores;
    private readonly Dictionary<ExecutionSourceTargetObject, SemaphoreSlim> _targetSemaphores;

    protected Execution Execution { get; }

    protected Dictionary<StepExecution, ExtendedCancellationTokenSource> CancellationTokenSources { get; }

    /// <summary>
    /// Internal enum used by the orchestrator to group step execution statuses on a less detailed level.
    /// The level of the internal enum is sufficient to keep track of when and if to execute steps.
    /// </summary>
    protected enum ExecutionStatus
    {
        NotStarted,
        Running,
        Succeeded,
        Failed
    };

    protected Dictionary<StepExecution, ExecutionStatus> StepStatuses { get; }

    public OrchestratorBase(
        ILogger<OrchestratorBase> logger,
        IExecutionConfiguration executionConfiguration,
        IStepExecutorFactory stepExecutorFactory,
        IDbContextFactory<BiflowContext> dbContextFactory,
        Execution execution)
    {
        _logger = logger;
        _executionConfig = executionConfiguration;
        _stepExecutorFactory = stepExecutorFactory;
        _dbContextFactory = dbContextFactory;
        Execution = execution;

        CancellationTokenSources = Execution.StepExecutions
            .ToDictionary(e => e, _ => new ExtendedCancellationTokenSource());
        StepStatuses = Execution.StepExecutions
            .ToDictionary(e => e, _ => ExecutionStatus.NotStarted);

        // If MaxParallelSteps was defined for the job, use that. Otherwise default to the value from configuration.
        var maxParallelStepsMain = execution.MaxParallelSteps > 0 ? execution.MaxParallelSteps : _executionConfig.MaxParallelSteps;
        _mainSemaphore = new SemaphoreSlim(maxParallelStepsMain, maxParallelStepsMain);

        // Create a Dictionary with max parallel steps for each step type.
        _stepTypeSemaphores = Enum.GetValues<StepType>()
            .ToDictionary(type => type, type =>
            {
                // Default to the main value of max parallel steps if the setting was not defined for the step type.
                var typeConcurrency = execution.ExecutionConcurrencies.FirstOrDefault(c => c.StepType == type)?.MaxParallelSteps;
                var maxParallelSteps = typeConcurrency > 0 ? (int)typeConcurrency : maxParallelStepsMain;
                return new SemaphoreSlim(maxParallelSteps, maxParallelSteps);
            });

        // Create a Dictionary with max concurrent steps for each target.
        // This allows only a predefined number of steps to write to the same target concurrently.
        var targets = Execution.StepExecutions
            .SelectMany(e => e.Targets)
            .Where(t => t.MaxConcurrentWrites > 0)
            .Distinct();
        _targetSemaphores = targets.ToDictionary(t => t, t => new SemaphoreSlim(t.MaxConcurrentWrites, t.MaxConcurrentWrites));
    }

    public abstract Task RunAsync();

    public void CancelExecution(string username)
    {
        // Cancel all steps
        foreach (var pair in CancellationTokenSources)
        {
            pair.Value.Cancel(username);
        }
    }

    public void CancelExecution(string username, Guid stepId)
    {
        // Cancel just one step
        var step = Execution.StepExecutions.FirstOrDefault(e => e.StepId == stepId);
        if (step is not null && CancellationTokenSources.TryGetValue(step, out var cts))
        {
            cts.Cancel(username);
        }
    }

    protected async Task StartNewStepWorkerAsync(StepExecution step)
    {
        // Update the step's status to Queued.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            foreach (var attempt in step.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Queued;
                context.Attach(attempt);
                context.Entry(attempt).Property(p => p.ExecutionStatus).IsModified = true;
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {step} Error updating step execution's status to Queued", Execution.ExecutionId, step);
        }

        // Wait until the semaphores can be entered and the step can be started.
        // Start from the most detailed semaphores and move towards the main semaphore.
        foreach (var target in step.Targets)
        {
            // If the target has a max no of concurrent writes defined, wait until the target semaphore can be entered.
            if (_targetSemaphores.TryGetValue(target, out var semaphore))
            {
                await semaphore.WaitAsync();
            }
        }
        await _stepTypeSemaphores[step.StepType].WaitAsync();
        await _mainSemaphore.WaitAsync();

        // Create a new step worker and start it asynchronously.
        var executor = _stepExecutorFactory.Create(step);
        
        _logger.LogInformation("{ExecutionId} {step} Started step execution", Execution.ExecutionId, step);
        bool result = false;
        try
        {
            // Wait for the step to finish.
            result = await executor.RunAsync(CancellationTokenSources[step]);
        }
        catch (Exception ex)
        {
            // Handle errors here that might not have been handled in the base executor.
            try
            {
                var attempt = step.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
                if (attempt is null) return; // return is allowed here because the finally block is executed anyway.
                using var context = _dbContextFactory.CreateDbContext();
                attempt.ExecutionStatus = StepExecutionStatus.Failed;
                attempt.StartDateTime ??= DateTimeOffset.Now;
                attempt.EndDateTime = DateTimeOffset.Now;
                attempt.ErrorMessage = $"Unhandled error caught in base orchestrator:\n\n{ex.Message}\n\n{ex.StackTrace}\n\n{attempt.ErrorMessage}";
                context.Attach(attempt).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            catch { }
        }
        finally
        {
            // Update the status.
            StepStatuses[step] = result ? ExecutionStatus.Succeeded : ExecutionStatus.Failed;
            
            // Release the semaphores once to make room for new parallel executions.
            _mainSemaphore.Release();
            _stepTypeSemaphores[step.StepType].Release();
            foreach (var target in step.Targets)
            {
                if (_targetSemaphores.TryGetValue(target, out var semaphore))
                {
                    semaphore.Release();
                }
            }

            _logger.LogInformation("{ExecutionId} {step} Finished step execution", Execution.ExecutionId, step);
        }
    }

}
