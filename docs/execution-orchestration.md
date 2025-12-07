### Execution Orchestration Flow (Biflow.Executor.Core)

This document explains, from code perspective, how a job execution flows through the orchestration stack when `IExecutionManager.StartExecutionAsync(executionId)` is called, and how the orchestrators interact via the orchestration eventing system.

Key namespaces and files referenced below live under `src\Biflow.Executor.Core` unless otherwise noted.

This document was generated with the help of AI.

---

#### 1) Entry point: IExecutionManager → ExecutionManager → JobExecutor

- Callers initiate with `IExecutionManager.StartExecutionAsync(Guid executionId)`.
  - File: `ExecutionManager\IExecutionManager.cs`
  - The default method creates an `OrchestrationContext` with:
    - `ExecutionId = executionId`
    - `ParentExecutionId = null`
    - `SynchronizedExecution = false`
  - It forwards to the internal overload `StartExecutionAsync(OrchestrationContext, CancellationToken)`.

- ExecutionManager creates and runs a JobExecutor task
  - File: `ExecutionManager\ExecutionManager.cs`
  - Uses `IJobExecutorFactory.CreateAsync(executionId, ct)` with retry (Polly) to construct a `JobExecutor` loaded with the full `Execution` aggregate from DB.
  - Stores the `IJobExecutor` and its running task in dictionaries and monitors completion.
  - On shutdown, cancels outstanding tasks via `_shutdownCts`.

- JobExecutor performs pre-flight and hands off to JobOrchestrator
  - File: `JobExecutor\JobExecutor.cs`
  - Updates `Execution` status to Running; runs validations (`IExecutionValidator`); evaluates execution parameter expressions; arms long-running notification timer.
  - Creates a `IJobOrchestrator` via `IJobOrchestratorFactory` and calls `_jobOrchestrator.RunAsync(context, shutdownToken)`.
  - After orchestration finishes, calculates final job status, updates end time, and sends completion notifications.

---

#### 2) JobOrchestrator: builds observers and registers into GlobalOrchestrator

- File: `JobOrchestrator\JobOrchestrator.cs`
  - Constructs one `OrchestrationObserver` per `StepExecution` in the `Execution`.
    - Each observer receives a set of `IOrchestrationTracker`s tailored to the step:
      - Always present: `DuplicateExecutionTracker`.
      - Based on execution mode: `ExecutionPhaseTracker` and/or `DependencyTracker`.
      - Target/data-object synchronization: `TargetTracker`.
      - Integration-specific trackers (e.g., `FunctionAppTracker`, `SqlConnectionTracker`, `PipelineClientTracker`, `ProxyTracker`).
    - Each observer also receives a `CancellationContext` combining per-step user cancellation token and the service shutdown token.
  - Calls `IGlobalOrchestrator.RegisterStepsAndObserversAsync(context, observers, stepExecutionListener: this)`.
  - Implements `IStepExecutionListener` to manage concurrency gates for steps:
    - A main semaphore limiting total parallel steps (job-level MaxParallelSteps or effectively unlimited).
    - Optional per-step-type semaphores from `Execution.ExecutionConcurrencies`. Entered semaphores are tracked per step and released in `OnPostExecuteAsync`.
  - Enforces job-level timeout: if exceeded (or on service shutdown) initiates user cancellations for all steps.

---

#### 3) GlobalOrchestrator: event hub and status authority

- File: `Orchestrator\GlobalOrchestrator.cs`
  - Maintains global state and eventing:
    - `_stepStatuses: Dictionary<StepExecution, OrchestrationStatus>` holds NotStarted/Running/Failed for all participating steps (including synchronized child executions; see below).
    - `_observers: List<IOrchestrationObserver>` is the set of subscribed observers to receive status updates.
    - `_childExecutions: Dictionary<Guid, List<Guid>>` maps a parent execution id to the execution ids of synchronized child executions so their status changes can be included in dependency computations.

- Registering steps and observers
  - `RegisterStepsAndObserversAsync(context, observers, listener)`:
    1) Locks state; adds entries for each incoming observer with status NotStarted.
    2) Feeds each new observer a snapshot of relevant statuses as `OrchestrationUpdate` items:
       - Includes statuses for steps belonging to the same synchronized execution family: if the current execution is a synchronized child, the parent’s group is also considered.
       - Otherwise, includes only NotStarted/Running steps (to prevent interference from long-lived completions outside the parent-child scope).
    3) Each observer’s `RegisterInitialUpdates` may immediately request execution via an `executeCallback`.
       - If so, GlobalOrchestrator transitions that step to Running and schedules `ExecuteStepAsync`.
    4) If execution wasn’t immediately requested, GlobalOrchestrator starts `WaitForProcessingAsync` on the observer. When the observer later requests processing, it calls back `OnStepReadyForProcessingAsync`, which either:
       - Executes the step (ExecuteAction), or
       - Cancels/fails/ignores the step with appropriate status updates (CancelAction/FailAction/IgnoreAction).
    5) After initial lifecycle methods, observers subscribe to the orchestrator to receive live updates (`Subscribe(this)`), which returns an `Unsubscriber` used by observers to stop updates when done.
    6) Any `StepExecutionMonitor`s returned by observers during registration are asynchronously added via `AddMonitorsAsync`.

- Executing a step
  - `ExecuteStepAsync(context, stepExecution, listener, cancellationContext)`:
    - Calls `listener.OnPreExecuteAsync` to acquire concurrency semaphores (job-level and possibly per-step-type). If canceled while waiting, the listener releases any acquired semaphores and the step is marked stopped.
    - Invokes `IStepOrchestrator.RunAsync(context, stepExecution, cancellationContext)`.
    - On completion, releases semaphores via `listener.OnPostExecuteAsync` and updates global status to reflect the step outcome.

- Status updates and DB writes
  - GlobalOrchestrator functions like `UpdateExecutionCancelledAsync`, `UpdateExecutionFailedAsync`, and `UpdateStepAsync` persist results to DB with late-cancellation retry semantics using `Extensions.ExecuteWithLateCancellationRetryAsync(...)` (custom retry similar to Polly).

- Parent/child executions
  - If `OrchestrationContext` carries `ParentExecutionId` and `SynchronizedExecution = true`, the child execution’s steps join the parent’s status universe. Status snapshots and live updates for dependency evaluation include both parent and synchronized children, allowing cross-execution dependency tracking. Cleanup of statuses for child executions is deferred to the parent after its completion.

---

#### 4) Orchestration eventing: observers, observable, trackers, and actions

- Observer interfaces
  - `IOrchestrationObserver` (file: `Orchestrator\IOrchestrationObserver.cs`) exposes the observer lifecycle:
    - `RegisterInitialUpdates(updates, executeCallback)` returns `StepExecutionMonitor`s and may immediately request execution via `executeCallback`.
    - `WaitForProcessingAsync(processCallback)` suspends until the observer decides the next action, then calls `processCallback(OrchestratorAction, CancellationContext)`.
    - `Subscribe(IOrchestrationObservable)` to receive future `OnUpdate(OrchestrationUpdate)` events.
    - `OnIncomingStepExecutionUpdate(OrchestrationUpdate)` lets existing observers react to newly joined steps and possibly emit additional monitors.

- Observable
  - `IGlobalOrchestrator` implements `IOrchestrationObservable` exposing `Subscribe(IOrchestrationObserver)` and sending updates whenever `_stepStatuses` change via `UpdateStatus(step, status)`.
  - Subscriptions are managed with `Unsubscriber` (file: `Orchestrator\Unsubscriber.cs`).

- Concrete observer: OrchestrationObserver
  - File: `Orchestrator\OrchestrationObserver.cs`
  - Holds a `TaskCompletionSource<OrchestratorAction>` and a set of `IOrchestrationTracker`s.
  - On receiving a batch or live update, runs the trackers:
    - Each tracker produces either: Wait, Execute, Cancel, or Fail (see `Orchestrator\Actions.cs`).
    - The first decisive outcome among trackers (Fail/Cancel/Wait) short-circuits. If none decides, action defaults to Execute.
  - When a decisive action is determined, it sets the TCS, stops further subscriptions for that step, and hands control to GlobalOrchestrator’s callbacks.

- Trackers shape orchestration decisions
  - Examples (files under `OrchestrationTracker`):
    - `DuplicateExecutionTracker`: prevents duplicate concurrent runs of the same step.
    - `ExecutionPhaseTracker` or `DependencyTracker`: enforces order via phases or explicit dependencies.
    - `TargetTracker`: ensures exclusive write/sync to a target data object across jobs.
    - Integration-specific trackers may throttle based on external system concurrency.

- Monitors
  - Some trackers emit `StepExecutionMonitor`s that GlobalOrchestrator collects and adds via `AddMonitorsAsync`. These represent additional cross-cutting constraints to monitor during orchestration (e.g., external resource locks), and may influence subsequent tracker outcomes.

---

#### 5) StepOrchestrator: step lifecycle and branching

- File: `Orchestrator\StepOrchestrator.cs`
  - Entry: `RunAsync(OrchestrationContext, StepExecution, CancellationContext)` returns `bool` indicating whether the step was actually executed (true) or skipped/stopped/failed early (false).
  - Pre-execution setup:
    - If user/service cancellation already requested, marks step Stopped.
    - If step uses execution parameters, copies current execution parameter values to the step, and evaluates step parameter expressions. Writes persisted via EF Core bulk updates.
    - Copies relevant execution parameter values to execution-condition parameters.
    - Evaluates the step’s execution condition; if false, marks step Skipped.
  - Execution:
    - Creates or uses the current `StepExecutionAttempt` and marks it Running.
    - Calls the concrete `IStepExecutor` for the step type via `IStepExecutorProvider` (e.g., Sql, Package, Function, Pipeline, etc.).
    - Handles retries recursively in `ExecuteRecursivelyWithRetriesAsync` using retry policy configured on the step; logs and persists status transitions with late-cancellation retries.
  - Post-execution outcome:
    - On success: `UpdateExecutionSucceededAsync` persists `Succeeded`.
    - On failure: `UpdateExecutionFailedAsync` persists `Failed` or `DependenciesFailed` with collected errors.
    - On cancel/stop: `UpdateExecutionCancelledAsync` or `UpdateExecutionStoppedAsync` persist respective statuses.

Note: GlobalOrchestrator updates the in-memory orchestration status (`_stepStatuses`) around step execution boundaries (e.g., NotStarted → Running, then to a terminal state) which in turn notifies observers.

---

#### 6) How steps “diverge” to StepOrchestrator

1) Each step is represented by its own `OrchestrationObserver` participating in the global eventing loop.
2) When an observer determines it’s time to run (ExecuteAction), GlobalOrchestrator:
   - Updates the step’s orchestration status to Running, and
   - Invokes `ExecuteStepAsync(...)`, which bridges to `IStepOrchestrator.RunAsync(...)` for that specific step.
3) From that point, the step’s own execution proceeds independently through StepOrchestrator and its concrete `IStepExecutor`, while GlobalOrchestrator continues to coordinate other steps based on events and statuses.

This is the “divergence” where each step follows its own path, yet reports back via status updates that continue to influence other observers through trackers.

---

#### 7) Cancellation, timeouts, and shutdown

- Service shutdown: ExecutionManager passes a shutdown token to JobExecutor → JobOrchestrator → GlobalOrchestrator/StepOrchestrator. If triggered, JobOrchestrator cancels all steps via user cancellation tokens and GlobalOrchestrator/StepOrchestrator persist stopped/canceled statuses.
- Job timeout: JobOrchestrator sets a timeout (Execution.TimeoutMinutes). On expiry, it triggers user cancellations across steps.
- Per-step cancel: `IExecutionManager.CancelExecution(executionId, username[, stepId])` routes to `IJobExecutor.Cancel(...)` which calls `JobOrchestrator.CancelExecution(...)`. That cancels all steps or a specific step’s user token, causing StepOrchestrator to mark Stopped/Cancelled as appropriate.
- Late-cancellation retry: Status updates to DB use a custom retry helper to avoid failing to persist terminal statuses if cancellation is requested late in the cycle.

---

#### 8) Summary sequence (high-level)

1) IExecutionManager.StartExecutionAsync(executionId)
2) ExecutionManager creates JobExecutor and starts it
3) JobExecutor preps execution and calls JobOrchestrator.RunAsync
4) JobOrchestrator builds observers and calls GlobalOrchestrator.RegisterStepsAndObserversAsync
5) Observers get initial snapshot; trackers decide Wait/Execute/Cancel/Fail
6) For Execute: GlobalOrchestrator → ExecuteStepAsync → StepOrchestrator.RunAsync
7) StepOrchestrator uses the appropriate IStepExecutor to run the step, manages retries, and persists statuses
8) GlobalOrchestrator updates orchestration statuses and notifies all observers
9) Repeat 5–8 until all steps complete or the job is canceled/timeout
10) JobExecutor finalizes execution status and sends notifications

---

References (key files):
- `ExecutionManager\IExecutionManager.cs`
- `ExecutionManager\ExecutionManager.cs`
- `JobExecutor\JobExecutor.cs`
- `JobOrchestrator\JobOrchestrator.cs`
- `Orchestrator\GlobalOrchestrator.cs`
- `Orchestrator\StepOrchestrator.cs`
- `Orchestrator\OrchestrationObserver.cs`
- `Orchestrator\IOrchestrationObserver.cs`, `IOrchestrationObservable.cs`, `Actions.cs`, `Unsubscriber.cs`
- `OrchestrationTracker\*.cs`
