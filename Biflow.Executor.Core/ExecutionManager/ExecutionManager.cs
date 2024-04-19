using Biflow.Executor.Core.Exceptions;
using Biflow.Executor.Core.JobExecutor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core;

internal class ExecutionManager(ILogger<ExecutionManager> logger, IJobExecutorFactory jobExecutorFactory)
    : BackgroundService, IExecutionManager, IDisposable
{
    private readonly object _lock = new();
    private readonly ILogger<ExecutionManager> _logger = logger;
    private readonly IJobExecutorFactory _jobExecutorFactory = jobExecutorFactory;
    private readonly Dictionary<Guid, IJobExecutor> _jobExecutors = [];
    private readonly Dictionary<Guid, Task> _executionTasks = [];
    private readonly CancellationTokenSource _shutdownCts = new();

    public IEnumerable<Execution> CurrentExecutions
    {
        get
        {
            lock (_lock)
            {
                return _jobExecutors.Values.Select(e => e.Execution).ToArray();
            }
        }
    }

    public async Task StartExecutionAsync(Guid executionId)
    {
        // Check for shutdown and duplicate key before proceeding
        // to creating the job executor which is a heavy operation.
        if (_shutdownCts.IsCancellationRequested)
        {
            throw new ApplicationException("Cannot start new executions when service shutdown is requested.");
        }

        lock (_lock)
        {
            if (_jobExecutors.ContainsKey(executionId))
            {
                throw new DuplicateExecutionException(executionId);
            }
        }

        var jobExecutor = await _jobExecutorFactory.CreateAsync(executionId);

        lock (_lock)
        {
            // Check for shutdown and duplicate key again because the dictionary
            // or token might have changed after the executor was created and the previous lock was released.
            if (_shutdownCts.IsCancellationRequested)
            {
                throw new ApplicationException("Cannot start new executions when service shutdown is requested.");
            }
            if (_jobExecutors.ContainsKey(executionId))
            {
                throw new DuplicateExecutionException(executionId);
            }

            _jobExecutors[executionId] = jobExecutor;
            var task = jobExecutor.RunAsync(_shutdownCts.Token);
            _executionTasks[executionId] = task;
            _ = MonitorExecutionTaskAsync(task, executionId);
        }
    }

    public void CancelExecution(Guid executionId, string username)
    {
        IJobExecutor? executor;
        lock (_lock)
        {
            if (!_jobExecutors.TryGetValue(executionId, out executor))
            {
                throw new ExecutionNotFoundException(executionId, $"No execution with id {executionId} is being managed.");
            }
        }
        executor.Cancel(username);
    }

    public void CancelExecution(Guid executionId, string username, Guid stepId)
    {
        IJobExecutor? executor;
        lock (_lock)
        {
            if (!_jobExecutors.TryGetValue(executionId, out executor))
            {
                throw new ExecutionNotFoundException(executionId, $"No execution with id {executionId} is being managed.");
            }
        }
        executor.Cancel(username, stepId);
    }

    public bool IsExecutionRunning(Guid executionId)
    {
        lock (_lock)
        {
            return _jobExecutors.ContainsKey(executionId);
        }
    }

    public async Task WaitForTaskCompleted(Guid executionId, CancellationToken cancellationToken)
    {
        Task? task;
        lock (_lock)
        {
            if (!_executionTasks.TryGetValue(executionId, out task))
            {
                return;
            }
        }
        await task.WaitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken shutdownToken)
    {
        try
        {
            await Task.Delay(-1, shutdownToken);
        }
        finally
        {
            Task[] tasks;
            lock (_lock)
            {
                _shutdownCts.Cancel();
                tasks = [.. _executionTasks.Values];
            }
            await Task.WhenAll(tasks);
        }
    }

    private async Task MonitorExecutionTaskAsync(Task task, Guid executionId)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Execution with id {executionId} was canceled due to service shutdown", executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution with id {executionId}", executionId);
        }
        finally
        {
            lock (_lock)
            {
                _jobExecutors.Remove(executionId);
                _executionTasks.Remove(executionId);
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _shutdownCts.Dispose();
    }
}