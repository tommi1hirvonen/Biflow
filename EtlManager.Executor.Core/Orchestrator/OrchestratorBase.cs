using EtlManager.DataAccess.Models;
using EtlManager.Executor.Core.Common;
using EtlManager.Executor.Core.StepExecutor;
using Serilog;

namespace EtlManager.Executor.Core.Orchestrator;

public abstract class OrchestratorBase
{
    protected IExecutionConfiguration _executionConfig;
    private readonly IStepExecutorFactory _stepExecutorFactory;

    protected Execution Execution { get; }

    private SemaphoreSlim Semaphore { get; }

    protected Dictionary<StepExecution, ExtendedCancellationTokenSource> CancellationTokenSources { get; }

    protected enum ExecutionStatus
    {
        NotStarted,
        Running,
        Success,
        Failed
    };

    protected Dictionary<StepExecution, ExecutionStatus> StepStatuses { get; }

    public OrchestratorBase(IExecutionConfiguration executionConfiguration, IStepExecutorFactory stepExecutorFactory, Execution execution)
    {
        _executionConfig = executionConfiguration;
        _stepExecutorFactory = stepExecutorFactory;
        Execution = execution;

        CancellationTokenSources = Execution.StepExecutions
            .ToDictionary(e => e, _ => new ExtendedCancellationTokenSource());
        StepStatuses = Execution.StepExecutions
            .ToDictionary(e => e, _ => ExecutionStatus.NotStarted);

        // If MaxParallelSteps was defined for the job, use that. Otherwise default to the value from configuration.
        var maxParallelSteps = execution.MaxParallelSteps > 0 ? execution.MaxParallelSteps : _executionConfig.MaxParallelSteps;
        Semaphore = new SemaphoreSlim(maxParallelSteps, maxParallelSteps);
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
        await Semaphore.WaitAsync();
        // Create a new step worker and start it asynchronously.
        var executor = _stepExecutorFactory.Create(step);
        var task = executor.RunAsync(CancellationTokenSources[step]);
        Log.Information("{ExecutionId} {step} Started step execution", Execution.ExecutionId, step);
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
            Semaphore.Release();
            Log.Information("{ExecutionId} {step} Finished step execution", Execution.ExecutionId, step);
        }
    }

}
