using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Common;
using EtlManager.Executor.Core.StepExecutor;
using Microsoft.Extensions.Logging;

namespace EtlManager.Executor.Core.Orchestrator;

internal abstract class OrchestratorBase
{
    private readonly ILogger<OrchestratorBase> _logger;
    private readonly IExecutionConfiguration _executionConfig;
    private readonly IStepExecutorFactory _stepExecutorFactory;
    private readonly SemaphoreSlim _mainSemaphore;
    private readonly Dictionary<StepType, SemaphoreSlim> _stepTypeSemaphores;

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

    public OrchestratorBase(ILogger<OrchestratorBase> logger, IExecutionConfiguration executionConfiguration, IStepExecutorFactory stepExecutorFactory, Execution execution)
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
        var maxParallelSteps = execution.MaxParallelSteps > 0 ? execution.MaxParallelSteps : _executionConfig.MaxParallelSteps;
        _mainSemaphore = new SemaphoreSlim(maxParallelSteps, maxParallelSteps);

        // Create a Dictionary with max parallel steps for each step type.
        _stepTypeSemaphores = Enum.GetValues<StepType>()
            .ToDictionary(type => type, _ =>
            {
                // TODO Calculate max parallel steps for each step type before creating the semaphore.
                return new SemaphoreSlim(maxParallelSteps, maxParallelSteps);
            });
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
        // Wait until the semaphore can be entered and the step can be started.
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
            
            // Release the semaphore once to make room for new parallel executions.
            _mainSemaphore.Release();
            _stepTypeSemaphores[step.StepType].Release();

            _logger.LogInformation("{ExecutionId} {step} Finished step execution", Execution.ExecutionId, step);
        }
    }

}
