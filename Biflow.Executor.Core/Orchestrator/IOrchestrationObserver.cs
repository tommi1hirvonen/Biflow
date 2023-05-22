using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestrationObserver
{
    public StepExecution StepExecution { get; }

    /// <summary>
    /// Called once to provide current snapshot of global orchestration step execution statuses
    /// </summary>
    /// <param name="initialStatuses"></param>
    public void RegisterInitialUpdates(IEnumerable<OrchestrationUpdate> initialStatuses);

    /// <summary>
    /// Called after RegisterInitialStepExecutionStatuses()
    /// </summary>
    /// <param name="observable"></param>
    public void Subscribe(IOrchestrationObservable observable);

    /// <summary>
    /// Called after Subscribe()
    /// </summary>
    /// <param name="listener"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task WaitForProcessingAsync(IStepReadyForProcessingListener listener);

    /// <summary>
    /// Called once on every global orchestration step execution status change until the observer unsubscribes from the provider
    /// </summary>
    /// <param name="statusChange"></param>
    public void OnUpdate(OrchestrationUpdate statusChange);
}
