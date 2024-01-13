using Biflow.Core.Entities.Steps.Execution;
using Biflow.Executor.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.Orchestrator;

internal class JobOrchestrator : IJobOrchestrator
{
    private readonly ILogger<JobOrchestrator> _logger;
    private readonly IGlobalOrchestrator _globalOrchestrator;
    private readonly SemaphoreSlim _mainSemaphore;
    private readonly Dictionary<StepType, SemaphoreSlim> _stepTypeSemaphores;
    private readonly Dictionary<ExecutionDataObject, SemaphoreSlim> _targetSemaphores;
    private readonly Execution _execution;
    private readonly Dictionary<StepExecution, ExtendedCancellationTokenSource> _cancellationTokenSources;

    public JobOrchestrator(
        ILogger<JobOrchestrator> logger,
        IOptionsMonitor<ExecutionOptions> options,
        IGlobalOrchestrator globalOrchestrator,
        Execution execution)
    {
        _logger = logger;
        _globalOrchestrator = globalOrchestrator;
        _execution = execution;
        _cancellationTokenSources = _execution.StepExecutions
            .ToDictionary(e => e, _ => new ExtendedCancellationTokenSource());

        // If MaxParallelSteps was defined for the job, use that. Otherwise default to the value from configuration.
        var maxParallelStepsMain = execution.MaxParallelSteps > 0 ? execution.MaxParallelSteps : options.CurrentValue.MaximumParallelSteps;
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
        var targets = _execution.StepExecutions
            .SelectMany(e => e.DataObjects)
            .Where(d => d.ReferenceType == DataObjectReferenceType.Target)
            .Select(t => t.DataObject)
            .Where(t => t.MaxConcurrentWrites > 0)
            .Distinct();
        _targetSemaphores = targets.ToDictionary(t => t, t => new SemaphoreSlim(t.MaxConcurrentWrites, t.MaxConcurrentWrites));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var observers = _execution.StepExecutions
            .Select(step =>
            {
                var listener = new StepProcessingListener(this, step);
                IOrchestrationObserver observer = step.Execution.DependencyMode
                    ? new DependencyModeObserver(step, listener, _cancellationTokenSources[step])
                    : new ExecutionPhaseModeObserver(step, listener, _cancellationTokenSources[step]);
                return observer;
            })
            .ToList();
        var orchestrationTask = _globalOrchestrator.RegisterStepsAndObservers(observers);
        // CancellationToken is triggered when the executor service is being shut down
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var waitTask = Task.Delay(-1, cts.Token);
        await Task.WhenAny(orchestrationTask, waitTask);
        // If shutdown was requested before the orchestration task finished
        if (cancellationToken.IsCancellationRequested)
        {
            CancelExecution("Executor service shutdown");
        }
        cts.Cancel(); // Cancel
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

    private class StepProcessingListener(JobOrchestrator instance, StepExecution stepExecution) : IStepExecutionListener
    {
        private readonly JobOrchestrator _instance = instance;
        private readonly StepExecution _stepExecution = stepExecution;
        private readonly List<SemaphoreSlim> _enteredSemaphores = [];

        public async Task OnPreExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource.Token;

            // Wait until the semaphores can be entered and the step can be started.
            // Start from the most detailed semaphores and move towards the main semaphore.
            // Keep track of semaphores that have been entered. If the step is stopped/canceled
            // while waiting to enter one of the semaphores, they can be released afterwards.
            var targets = _stepExecution.DataObjects
                .Where(d => d.ReferenceType == DataObjectReferenceType.Target)
                .Select(d => d.DataObject);
            foreach (var target in targets)
            {
                // If the target has a max no of concurrent writes defined, wait until the target semaphore can be entered.
                if (_instance._targetSemaphores.TryGetValue(target, out var semaphore))
                {
                    await semaphore.WaitAsync(cancellationToken);
                    _enteredSemaphores.Add(semaphore);
                }
            }
            var stepTypeSemaphore = _instance._stepTypeSemaphores[_stepExecution.StepType];
            await stepTypeSemaphore.WaitAsync(cancellationToken);
            _enteredSemaphores.Add(stepTypeSemaphore);
            await _instance._mainSemaphore.WaitAsync(cancellationToken);
            _enteredSemaphores.Add(_instance._mainSemaphore);
        }

        public Task OnPostExecuteAsync()
        {
            // Release the semaphores once to make room for new parallel executions.
            foreach (var semaphore in _enteredSemaphores)
            {
                semaphore.Release();
            }

            _instance._logger.LogInformation("{ExecutionId} {step} Finished step execution", _instance._execution.ExecutionId, _stepExecution);
            return Task.CompletedTask;
        }
    }

}
