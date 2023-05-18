using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Biflow.Executor.Core.Orchestrator;

internal class JobOrchestrator
{
    private readonly ILogger<JobOrchestrator> _logger;
    private readonly IExecutionConfiguration _executionConfig;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    private readonly IGlobalOrchestrator _globalOrchestrator;
    private readonly SemaphoreSlim _mainSemaphore;
    private readonly Dictionary<StepType, SemaphoreSlim> _stepTypeSemaphores;
    private readonly Dictionary<ExecutionDataObject, SemaphoreSlim> _targetSemaphores;
    private readonly Execution _execution;
    private readonly Dictionary<StepExecution, ExtendedCancellationTokenSource> _cancellationTokenSources;

    public JobOrchestrator(
        ILogger<JobOrchestrator> logger,
        IExecutionConfiguration executionConfiguration,
        IDbContextFactory<BiflowContext> dbContextFactory,
        IGlobalOrchestrator globalOrchestrator,
        Execution execution)
    {
        _logger = logger;
        _executionConfig = executionConfiguration;
        _dbContextFactory = dbContextFactory;
        _globalOrchestrator = globalOrchestrator;
        _execution = execution;
        _cancellationTokenSources = _execution.StepExecutions
            .ToDictionary(e => e, _ => new ExtendedCancellationTokenSource());

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
        var targets = _execution.StepExecutions
            .SelectMany(e => e.Targets)
            .Where(t => t.MaxConcurrentWrites > 0)
            .Distinct();
        _targetSemaphores = targets.ToDictionary(t => t, t => new SemaphoreSlim(t.MaxConcurrentWrites, t.MaxConcurrentWrites));
    }

    public async Task RunAsync()
    {
        var observers = _execution.StepExecutions
            .Select(step =>
            {
                var listener = new OrchestrationListener(this, step);
                return new StepExecutionStatusObserver(step, listener, _cancellationTokenSources[step]);
            })
            .ToList();
        var tasks = _globalOrchestrator.RegisterStepsAndObservers(observers);
        await Task.WhenAll(tasks);
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

    private class OrchestrationListener : IOrchestrationListener
    {
        private readonly JobOrchestrator _instance;
        private readonly StepExecution _stepExecution;
        private readonly List<SemaphoreSlim> _enteredSemaphores = new();

        public OrchestrationListener(JobOrchestrator instance, StepExecution stepExecution)
        {
            _instance = instance;
            _stepExecution = stepExecution;
        }

        public Task OnPreQueuedAsync(IStepOrchestrationContext context, StepAction stepAction)
        {
            if (stepAction == StepAction.FailDuplicate)
            {
                context.ShouldFailWithStatus(StepExecutionStatus.Duplicate);
            }
            else if (stepAction == StepAction.FailDependencies)
            {
                context.ShouldFailWithStatus(StepExecutionStatus.DependenciesFailed);
            }
            else if (stepAction == StepAction.Wait)
            {
                throw new ArgumentException($"Incorrect StepAction {stepAction} for step {_stepExecution.StepId} when entering orchestration");
            }
            return Task.CompletedTask;
        }

        public async Task OnPreExecuteAsync(IStepOrchestrationContext context, ExtendedCancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource.Token;

            // Wait until the semaphores can be entered and the step can be started.
            // Start from the most detailed semaphores and move towards the main semaphore.
            // Keep track of semaphores that have been entered. If the step is stopped/canceled
            // while waiting to enter one of the semaphores, they can be released afterwards.
            foreach (var target in _stepExecution.Targets)
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

        public Task OnPostExecuteAsync(IStepOrchestrationContext context)
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
