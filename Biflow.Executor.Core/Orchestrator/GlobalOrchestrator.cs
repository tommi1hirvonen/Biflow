using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Biflow.Executor.Core.StepExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Orchestrator;

internal class GlobalOrchestrator : IGlobalOrchestrator
{
    private readonly ILogger<GlobalOrchestrator> _logger;
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;
    private readonly IStepExecutorFactory _stepExecutorFactory;
    private readonly List<IObserver<StepExecutionStatusInfo>> _observers = new();
    private readonly Dictionary<StepExecution, OrchestrationStatus> _steps = new();

    public GlobalOrchestrator(
        ILogger<GlobalOrchestrator> logger,
        IDbContextFactory<BiflowContext> dbContextFactory,
        IStepExecutorFactory stepExecutorFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _stepExecutorFactory = stepExecutorFactory;
    }

    public Task RegisterStepExecutionAsync(
        StepExecution stepExecution,
        Func<StepAction, Task> onReadyForOrchestration,
        CancellationToken cancellationToken)
    {
        var observer = new StepExecutionStatusObserver(stepExecution, this);
        _steps[stepExecution] = OrchestrationStatus.NotStarted;
        return observer.WaitForOrchestrationAsync(onReadyForOrchestration, cancellationToken);
    }

    public IDisposable Subscribe(IObserver<StepExecutionStatusInfo> observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
            foreach (var (step, status) in _steps)
            {
                observer.OnNext(new(step, status));
            }
        }
        return new Unsubscriber<StepExecutionStatusInfo>(_observers, observer);
    }

    public async Task QueueAsync(
        StepExecution stepExecution,
        Func<ExtendedCancellationTokenSource, Task> onPreExecute,
        Func<Task> onPostExecute,
        ExtendedCancellationTokenSource cts)
    {
        UpdateStatus(stepExecution, OrchestrationStatus.Running);

        // Update the step's status to Queued.
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            foreach (var attempt in stepExecution.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Queued;
                context.Attach(attempt);
                context.Entry(attempt).Property(p => p.ExecutionStatus).IsModified = true;
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ExecutionId} {step} Error updating step execution's status to Queued", stepExecution.ExecutionId, stepExecution);
        }

        bool result = false;
        try
        {
            await onPreExecute(cts);

            // Create a new step worker.
            var executor = _stepExecutorFactory.Create(stepExecution);
            // Execute the worker and capture the result.
            var task = executor.RunAsync(cts);
            result = await task;
        }
        catch (OperationCanceledException)
        {
            // We should only arrive here if the step was canceled while it was Queued.
            // If the step was canceled once its execution had started,
            // then the step's executor should handle the cancellation and the result is returned normally from RunAsync().
            await UpdateExecutionCancelledAsync(stepExecution, cts.Username);
        }
        catch (Exception ex)
        {
            try
            {
                await UpdateExecutionFailedAsync(ex, stepExecution);
            }
            catch { }
        }
        finally
        {
            var status = result ? OrchestrationStatus.Succeeded : OrchestrationStatus.Failed;
            UpdateStatus(stepExecution, status);
            await onPostExecute();
        }
    }

    public void UpdateStatus(StepExecution step, OrchestrationStatus status)
    {
        if (status == OrchestrationStatus.Succeeded || status == OrchestrationStatus.Failed)
        {
            _steps.Remove(step);
        }
        foreach (var observer in _observers)
        {
            observer.OnNext(new(step, status));
        }
    }

    private async Task UpdateExecutionCancelledAsync(StepExecution stepExecution, string username)
    {
        using var context = _dbContextFactory.CreateDbContext();
        foreach (var attempt in stepExecution.StepExecutionAttempts)
        {
            attempt.StartDateTime ??= DateTimeOffset.Now;
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.StoppedBy = username;
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync();
    }

    private async Task UpdateExecutionFailedAsync(Exception ex, StepExecution stepExecution)
    {
        var attempt = stepExecution.StepExecutionAttempts.MaxBy(e => e.RetryAttemptIndex);
        if (attempt is null) return; // return is allowed here because the finally block is executed anyway.
        using var context = _dbContextFactory.CreateDbContext();
        attempt.ExecutionStatus = StepExecutionStatus.Failed;
        attempt.StartDateTime ??= DateTimeOffset.Now;
        attempt.EndDateTime = DateTimeOffset.Now;
        attempt.ErrorMessage = $"Unhandled error caught in global orchestrator:\n\n{ex.Message}\n\n{ex.StackTrace}\n\n{attempt.ErrorMessage}";
        context.Attach(attempt).State = EntityState.Modified;
        await context.SaveChangesAsync();
    }

}
