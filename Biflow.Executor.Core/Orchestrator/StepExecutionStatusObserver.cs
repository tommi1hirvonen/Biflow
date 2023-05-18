using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Orchestrator;

/// <summary>
/// Observes orchestration status updates of multiple potential providers for a single StepExecution
/// </summary>
internal class StepExecutionStatusObserver : IObserver<StepExecutionStatusInfo>, IDisposable
{
    private readonly TaskCompletionSource<StepAction> _tcs = new();
    private readonly StepExecution _stepExecution;
    private IDisposable? _unsubscriber;
    private readonly Dictionary<StepExecution, OrchestrationStatus> _dependencies = new();
    private readonly Dictionary<StepExecution, OrchestrationStatus> _dependsOnThis = new();
    private readonly Dictionary<StepExecution, OrchestrationStatus> _duplicates = new();

    public StepExecutionStatusObserver(StepExecution stepExecution, IEnumerable<StepExecutionStatusInfo> initialStatuses)
    {
        _stepExecution = stepExecution;
        foreach (var status in initialStatuses)
        {
            HandleStatusInfo(status);
        }
        CheckExecutionEligibility();
    }

    public async Task WaitForOrchestrationAsync(Func<StepExecution, StepAction, Task> onReadyForOrchestration, CancellationToken cancellationToken)
    {
        // TODO Handle cancellation
        var stepAction = await _tcs.Task.WaitAsync(cancellationToken);
        await onReadyForOrchestration(_stepExecution, stepAction);
    }

    public void Subscribe(IObservable<StepExecutionStatusInfo> provider)
    {
        _unsubscriber = provider.Subscribe(this);
    }    

    public void Unsubscribe()
    {
        _unsubscriber?.Dispose();
        _unsubscriber = null;
    }

    public void OnNext(StepExecutionStatusInfo value)
    {
        HandleStatusInfo(value);
        CheckExecutionEligibility();
    }

    private void HandleStatusInfo(StepExecutionStatusInfo value)
    {
        var (step, status) = value;
        if (step.StepId == _stepExecution.StepId && step.ExecutionId != _stepExecution.ExecutionId)
        {
            _duplicates[step] = status;
        }
        else if (_stepExecution.ExecutionDependencies.Any(d => d.DependantOnStepId == step.StepId))
        {
            _dependencies[step] = status;
        }
        else if (step.ExecutionDependencies.Any(d => d.DependantOnStepId == _stepExecution.StepId))
        {
            _dependsOnThis[step] = status;
        }
    }

    private void CheckExecutionEligibility()
    {
        var action = CalculateStepAction();
        if (action != StepAction.Wait)
        {
            Unsubscribe();
            _tcs.TrySetResult(action);
        }     
    }

    private StepAction CalculateStepAction()
    {
        if (_duplicates.Any(d => d.Value == OrchestrationStatus.Running) &&
            _stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Fail)
        {
            return StepAction.FailDuplicate;
        }

        if (_duplicates.Any(d => d.Value == OrchestrationStatus.Running) &&
            _stepExecution.DuplicateExecutionBehaviour == DuplicateExecutionBehaviour.Wait)
        {
            return StepAction.Wait;
        }

        if (_dependsOnThis.Any(d => d.Value == OrchestrationStatus.Running))
        {
            return StepAction.Wait;
        }

        var onSucceeded = _stepExecution.ExecutionDependencies
            .Where(d => d.DependencyType == DependencyType.OnSucceeded)
            .Select(d => d.DependantOnStepId);

        var onFailed = _stepExecution.ExecutionDependencies
            .Where(d => d.DependencyType == DependencyType.OnFailed)
            .Select(d => d.DependantOnStepId);

        var dependencies = _dependencies.Select(d => new { d.Key.StepId, Status = d.Value });

        // If there are any on-success dependencies, which have been marked as failed
        // OR
        // if there are any on-failed dependencies, which have been marked as succeeded, skip this step.
        if (onSucceeded.Any(d1 => dependencies.Any(d2 => d2.Status == OrchestrationStatus.Failed && d2.StepId == d1)) ||
            onFailed.Any(d1 => dependencies.Any(d2 => d2.Status == OrchestrationStatus.Succeeded && d2.StepId == d1)))
        {
            return StepAction.FailDependencies;
        }
        // No reason to skip this step.
        // If all the step's dependencies have been completed (success or failure), the step can be executed.
        else if (_dependencies.All(d => d.Value == OrchestrationStatus.Succeeded || d.Value == OrchestrationStatus.Failed))
        {
            return StepAction.Execute;
        }

        // No action should be taken with this step at this time. Wait until next round.
        return StepAction.Wait;
    }

    public void Dispose() => _unsubscriber?.Dispose();

    #region NotImplemented

    // No implementation needed: Method is not called by the StepExecutionStatusProvider class
    public void OnCompleted()
    {
        throw new NotImplementedException();
    }

    // No implementation needed: Method is not called by the StepExecutionStatusProvider class
    public void OnError(Exception error)
    {
        throw new NotImplementedException();
    }
    #endregion
}
