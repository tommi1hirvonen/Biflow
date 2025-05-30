﻿using System.Collections.Concurrent;
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
    private readonly Dictionary<StepExecution, ExtendedCancellationTokenSource> _cancellationTokenSources;
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
            .ToDictionary(e => e, _ => new ExtendedCancellationTokenSource());

        // If MaxParallelSteps was defined for the job, use that.
        // Otherwise, default to int.MaxValue, i.e. practically no upper limit.
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

    public async Task RunAsync(OrchestrationContext context, CancellationToken cancellationToken)
    {
        var observers = _execution.StepExecutions
            .Select(step =>
            {
                var trackers = GenerateOrchestrationTrackers(step).ToArray();
                var observer = new OrchestrationObserver(
                    logger: _logger,
                    stepExecution: step,
                    orchestrationTrackers: trackers,
                    cancellationTokenSource: _cancellationTokenSources[step]);
                return observer;
            })
            .ToArray();
        var orchestrationTask = _globalOrchestrator.RegisterStepsAndObserversAsync(context, observers,
            stepExecutionListener: this);
        
        // CancellationToken is triggered when the executor service is being shut down
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (_execution.TimeoutMinutes > 0)
        {
            cts.CancelAfter(TimeSpan.FromMinutes(_execution.TimeoutMinutes));
        }
        
        var waitTask = Task.Delay(-1, cts.Token);
        await Task.WhenAny(orchestrationTask, waitTask);
        
        // If shutdown was requested before the orchestration task finished
        if (cancellationToken.IsCancellationRequested)
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
        // Cancel all steps
        foreach (var pair in _cancellationTokenSources)
        {
            pair.Value.Cancel(username);
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

    public async Task OnPreExecuteAsync(StepExecution stepExecution,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;

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
        // 1. Check for duplicate executions of the step
        // 2. Check execution phases or dependencies or both, depending on the execution mode.
        // 3. Check whether potential target data objects are already being written to or
        // that their concurrency allows more steps to be executed.
        // 4. Check integration specific concurrencies.
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
        }
    }
}
