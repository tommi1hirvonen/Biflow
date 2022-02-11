using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal abstract class OrchestratorBase
{
    private readonly ILogger<OrchestratorBase> _logger;
    private readonly IExecutionConfiguration _executionConfig;
    private readonly IStepExecutorFactory _stepExecutorFactory;
    private readonly SemaphoreSlim _mainSemaphore;
    private readonly Dictionary<StepType, SemaphoreSlim> _stepTypeSemaphores;
    private readonly Dictionary<ExecutionSourceTargetObject, SemaphoreSlim> _targetSemaphores;

    protected Execution Execution { get; }

    protected Dictionary<StepExecution, ExtendedCancellationTokenSource> CancellationTokenSources { get; }

    protected enum ExecutionStatus
    {
        NotStarted,
        Running,
        Success,
        Failed
    };

    protected Dictionary<StepExecution, ExecutionStatus> StepStatuses { get; }

    public OrchestratorBase(
        ILogger<OrchestratorBase> logger,
        IExecutionConfiguration executionConfiguration,
        IStepExecutorFactory stepExecutorFactory,
        Execution execution)
    {
        _logger = logger;
        _executionConfig = executionConfiguration;
        _stepExecutorFactory = stepExecutorFactory;
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
        if (step is not null && CancellationTokenSources.ContainsKey(step))
        {
            CancellationTokenSources[step].Cancel(username);
        }
    }

    protected async Task StartNewStepWorkerAsync(StepExecution step)
    {
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
        var task = executor.RunAsync(CancellationTokenSources[step]);
        
        _logger.LogInformation("{ExecutionId} {step} Started step execution", Execution.ExecutionId, step);
        bool result = false;
        try
        {
            // Wait for the step to finish.
            result = await task;
        }
        finally
        {
            // Update the status.
            StepStatuses[step] = result ? ExecutionStatus.Success : ExecutionStatus.Failed;
            
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
