using System.Collections.Concurrent;
using Biflow.Executor.Core.OrchestrationTracker;
using Biflow.Executor.Core.Orchestrator;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.JobOrchestrator;

internal class JobOrchestrator : IJobOrchestrator, IStepExecutionListener
{
    private readonly ILogger<JobOrchestrator> _logger;
    private readonly IGlobalOrchestrator _globalOrchestrator;
    private readonly SemaphoreSlim _mainSemaphore;
    private readonly Dictionary<StepType, SemaphoreSlim> _stepTypeSemaphores;
    private readonly Execution _execution;
    private readonly Dictionary<StepExecution, UserCancellationTokenSource> _cancellationTokenSources;
    private readonly ConcurrentDictionary<StepExecution, List<SemaphoreSlim>> _enteredSemaphores = [];

    public JobOrchestrator(
        ILogger<JobOrchestrator> logger,
        IGlobalOrchestrator globalOrchestrator,
        Execution execution)
    {
        _logger = logger;
        _globalOrchestrator = globalOrchestrator;
        _execution = execution;
        _cancellationTokenSources = _execution.StepExecutions
            .ToDictionary(e => e, _ => new UserCancellationTokenSource());

        // If MaxParallelSteps was defined for the job, use that.
        // Otherwise, default to int.MaxValue, i.e., practically no upper limit.
        var maxParallelStepsMain = execution.MaxParallelSteps > 0
            ? execution.MaxParallelSteps
            : int.MaxValue;
        _mainSemaphore = new SemaphoreSlim(maxParallelStepsMain, maxParallelStepsMain);

        // Create a Dictionary with max parallel steps for each step type.
        _stepTypeSemaphores = execution.ExecutionConcurrencies
            .Where(c => c.MaxParallelSteps > 0)
            .DistinctBy(c => c.StepType)
            .ToDictionary(c => c.StepType, c => new SemaphoreSlim(c.MaxParallelSteps, c.MaxParallelSteps));
    }

    public async Task RunAsync(OrchestrationContext context, CancellationToken shutdownToken)
    {
        var observers = _execution.StepExecutions
            .Select(step =>
            {
                var trackers = GenerateOrchestrationTrackers(step).ToArray();
                var userCancellation = _cancellationTokenSources[step];
                var cancellationContext = new CancellationContext(userCancellation, shutdownToken);
                var observer = new OrchestrationObserver(
                    logger: _logger,
                    stepExecution: step,
                    orchestrationTrackers: trackers,
                    cancellationContext: cancellationContext);
                return observer;
            })
            .ToArray();
        var orchestrationTask = _globalOrchestrator.RegisterStepsAndObserversAsync(context, observers,
            stepExecutionListener: this);
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);

        if (_execution.TimeoutMinutes > 0)
        {
            cts.CancelAfter(TimeSpan.FromMinutes(_execution.TimeoutMinutes));
        }
        
        var waitTask = Task.Delay(-1, cts.Token);
        await Task.WhenAny(orchestrationTask, waitTask);
        
        // If shutdown was requested before the orchestration task finished
        if (shutdownToken.IsCancellationRequested)
        {
            CancelExecution("Executor service shutdown");
        }
        else if (cts.IsCancellationRequested)
        {
            CancelExecution("Job timeout limit reached");
        }

        // Just to be safe and correct, also cancel the linked token source even though it doesn't do much.
        if (!cts.IsCancellationRequested)
        {
            try { await cts.CancelAsync(); } catch { /* ignore any possible exceptions */ }
        }

        await orchestrationTask; // Wait for orchestration tasks to finish
    }

    public void CancelExecution(string username)
    {
        // Cancel all steps.
        // Start canceling in reverse topological order, i.e., cancel steps with the most dependencies first.
        // Otherwise, some steps might be marked with status DependenciesFailed before they are canceled.
        IEnumerable<KeyValuePair<StepExecution, UserCancellationTokenSource>> steps;
        try
        {
            var comparer = new TopologicalStepExecutionComparer(_cancellationTokenSources.Keys);
            steps = _cancellationTokenSources.OrderByDescending(e => e.Key, comparer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while doing topological sort for job cancellation");
            steps = _cancellationTokenSources.OrderByDescending(e => e.Key.ExecutionPhase);
        }
        foreach (var (_, cts) in steps)
        {
            cts.Cancel(username);
        }
    }

    public void CancelExecution(string username, Guid stepId)
    {
        // Cancel just one step
        var step = _execution.StepExecutions.FirstOrDefault(e => e.StepId == stepId);
        if (step is not null && _cancellationTokenSources.TryGetValue(step, out var cts))
        {
            cts.Cancel(username);
        }
    }

    public async Task OnPreExecuteAsync(StepExecution stepExecution, CancellationContext cancellationContext)
    {
        var cancellationToken = cancellationContext.CancellationToken;

        List<SemaphoreSlim> enteredSemaphores = [];
        _enteredSemaphores[stepExecution] = enteredSemaphores;

        // Wait until the semaphores can be entered and the step can be started.
        // Start from the most detailed semaphores and move towards the main semaphore.
        // Keep track of semaphores that have been entered. If the step is stopped/canceled
        // while waiting to enter one of the semaphores, they can be released afterward.

        if (_stepTypeSemaphores.TryGetValue(stepExecution.StepType, out var stepTypeSemaphore))
        {
            await stepTypeSemaphore.WaitAsync(cancellationToken);
            enteredSemaphores.Add(stepTypeSemaphore);
        }

        await _mainSemaphore.WaitAsync(cancellationToken);
        enteredSemaphores.Add(_mainSemaphore);
    }

    public Task OnPostExecuteAsync(StepExecution stepExecution)
    {
        foreach (var semaphore in _enteredSemaphores[stepExecution])
        {
            semaphore.Release();
        }

        _ = _enteredSemaphores.TryRemove(stepExecution, out _);

        _logger.LogInformation("{ExecutionId} {step} Finished step execution", _execution.ExecutionId, stepExecution);
        return Task.CompletedTask;
    }

    private static IEnumerable<IOrchestrationTracker> GenerateOrchestrationTrackers(StepExecution step)
    {
        // The order in which trackers are enumerated in OrchestrationObserver matters.
        // 1. Check for duplicate executions of step
        // 2. Check execution phases or dependencies or both, depending on the execution mode.
        // 3. Check whether potential target data objects are already being written to or
        // that their concurrency allows more steps to be executed.
        // 4. Check integration-specific concurrences.
        yield return new DuplicateExecutionTracker(step);
        switch (step.Execution.ExecutionMode)
        {
            case ExecutionMode.ExecutionPhase:
                yield return new ExecutionPhaseTracker(step);
                break;
            case ExecutionMode.Dependency:
                yield return new DependencyTracker(step);
                break;
            case ExecutionMode.Hybrid:
                yield return new ExecutionPhaseTracker(step);
                yield return new DependencyTracker(step);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    $"Unsupported execution mode {step.Execution.ExecutionMode} for execution id {step.ExecutionId}");
        }
        yield return new TargetTracker(step); // Handles target data object synchronization across job executions.
        switch (step)
        {
            case FunctionStepExecution function:
                yield return new FunctionAppTracker(function);
                break;
            case SqlStepExecution or PackageStepExecution:
                yield return new SqlConnectionTracker(step);
                break;
            case PipelineStepExecution pipeline:
                yield return new PipelineClientTracker(pipeline);
                break;
            case ExeStepExecution exe:
                yield return new ProxyTracker(exe);
                break;
        }
    }
}
