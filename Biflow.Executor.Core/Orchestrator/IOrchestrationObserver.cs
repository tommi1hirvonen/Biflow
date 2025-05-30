﻿namespace Biflow.Executor.Core.Orchestrator;

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
    /// <param name="executeCallback">callback to request immediate execution for the calling observer</param>
    public IEnumerable<StepExecutionMonitor> RegisterInitialUpdates(
        IEnumerable<OrchestrationUpdate> updates,
        Action<ExtendedCancellationTokenSource> executeCallback);

    /// <summary>
    /// Called after <see cref="RegisterInitialUpdates"/> if execute was not requested.
    /// This method is designed to suspend/await for a long time until the observer is ready to push the step execution for processing.
    /// </summary>
    /// <param name="processCallback">callback to request processing for the calling observer</param>
    public Task WaitForProcessingAsync(Func<OrchestratorAction, ExtendedCancellationTokenSource, Task> processCallback);

    /// <summary>
    /// Called after <see cref="WaitForProcessingAsync"/> if execution was not requested in <see cref="RegisterInitialUpdates"/>.
    /// </summary>
    /// <param name="observable"></param>
    public void Subscribe(IOrchestrationObservable observable);

    /// <summary>
    /// After the call to <see cref="Subscribe"/>, <see cref="OnUpdate"/> is called once on every global orchestration step execution status change until the observer unsubscribes from the provider.
    /// Called only if execute was not requested in <see cref="RegisterInitialUpdates"/>
    /// </summary>
    /// <param name="statusChange"></param>
    public void OnUpdate(OrchestrationUpdate statusChange);

    /// <summary>
    /// After the call to <see cref="Subscribe"/>, <see cref="OnIncomingStepExecutionUpdate"/> is called when new step executions
    /// are joining global orchestration. It is expected for existing observer to return possible new monitors caused by the joining step.
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public IEnumerable<StepExecutionMonitor> OnIncomingStepExecutionUpdate(OrchestrationUpdate update);
}
