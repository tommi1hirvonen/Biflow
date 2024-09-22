using Biflow.Executor.Core.Common;

namespace Biflow.Executor.Core.Orchestrator;

internal interface IOrchestrationObserver
{
    public StepExecution StepExecution { get; }

    /// <summary>
    /// Value denoting the priority of this observer.
    /// Lower value means higher priority.
    /// Higher priority observers will receive status updates first.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Called once as the first lifecycle method to provide current snapshot of global orchestration step execution statuses.
    /// </summary>
    /// <param name="updates"></param>
    public IEnumerable<StepExecutionMonitor> RegisterInitialUpdates(IEnumerable<OrchestrationUpdate> updates);

    /// <summary>
    /// Called potentially multiple times as the second lifecycle method to provide status updates
    /// of steps in the same execution that have been immediately started when the execution is registered in orchestration.
    /// </summary>
    /// <param name="update"></param>
    public void RegisterInitialUpdate(OrchestrationUpdate update);

    /// <summary>
    /// Called as the next lifecycle method after all calls to <see cref="RegisterInitialUpdate"/>.
    /// If the observer has no reason to wait for processing,
    /// it can request processing immediately from the listener parameter by returning a <see cref="Task"/>.
    /// In this case, the remaining <see cref="IOrchestrationObserver"/> lifecycle methods will not be called.
    /// </summary>
    /// <param name="listener"></param>
    /// <returns><see cref="Task"/> if the observer should be processed right after initial updates, <see langword="null"/> if not.</returns>
    public Task? AfterInitialUpdatesRegisteredAsync(Func<StepExecution, IStepExecutionListener, ExtendedCancellationTokenSource, Task> executeCallback);

    /// <summary>
    /// Called after <see cref="AfterInitialUpdatesRegisteredAsync"/> if processing was not requested.
    /// This method is designed to suspend/await for a long time until the observer is ready to push the step execution for processing.
    /// </summary>
    /// <param name="listener"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task WaitForProcessingAsync(Func<StepExecution, OrchestratorAction, IStepExecutionListener, ExtendedCancellationTokenSource, Task> processCallback);

    /// <summary>
    /// Called after <see cref="WaitForProcessingAsync"/> if processing was not requested in <see cref="AfterInitialUpdatesRegisteredAsync"/>.
    /// </summary>
    /// <param name="observable"></param>
    public void Subscribe(IOrchestrationObservable observable);

    /// <summary>
    /// After the call to <see cref="Subscribe"/>, <see cref="OnUpdate"/> is called once on every global orchestration step execution status change until the observer unsubscribes from the provider.
    /// Called only if processing was not requested in <see cref="AfterInitialUpdatesRegisteredAsync"/>
    /// </summary>
    /// <param name="statusChange"></param>
    public IEnumerable<StepExecutionMonitor> OnUpdate(OrchestrationUpdate statusChange);
}
