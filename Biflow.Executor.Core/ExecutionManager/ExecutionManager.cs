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
    private readonly AsyncQueue<Func<Task>> _backgroundTaskQueue = new();
    private readonly CancellationTokenSource _shutdownCts = new();

    public IEnumerable<Execution> CurrentExecutions => _jobExecutors.Values.Select(e => e.Execution);

    public async Task StartExecutionAsync(Guid executionId)
    {
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
            _jobExecutors[executionId] = jobExecutor;
        }

        var task = jobExecutor.RunAsync(executionId, _shutdownCts.Token);
        lock (_lock)
        {
            _executionTasks[executionId] = task;
        }

        _backgroundTaskQueue.Enqueue(() => MonitorExecutionTaskAsync(task, executionId));
    }

    public void CancelExecution(Guid executionId, string username)
    {
        IJobExecutor? value;
        lock (_lock)
        {
            if (!_jobExecutors.TryGetValue(executionId, out value))
            {
                throw new ExecutionNotFoundException(executionId, $"No execution with id {executionId} is being managed.");
            }
        }
        var executor = value;
        executor.Cancel(username);
    }

    public void CancelExecution(Guid executionId, string username, Guid stepId)
    {
        IJobExecutor? value;
        lock (_lock)
        {
            if (!_jobExecutors.TryGetValue(executionId, out value))
            {
                throw new ExecutionNotFoundException(executionId, $"No execution with id {executionId} is being managed.");
            }
        }
        var executor = value;
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
            await foreach (var taskDelegate in _backgroundTaskQueue.WithCancellation(shutdownToken))
            {
                _ = taskDelegate();
            }
        }
        finally
        {
            _shutdownCts.Cancel();
            await Task.WhenAll(_executionTasks.Values);
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