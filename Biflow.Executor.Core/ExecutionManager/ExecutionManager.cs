using Biflow.Executor.Core.JobExecutor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core;

internal class ExecutionManager(ILogger<ExecutionManager> logger, IJobExecutorFactory jobExecutorFactory)
    : BackgroundService, IExecutionManager
{
    private readonly ILogger<ExecutionManager> _logger = logger;
    private readonly IJobExecutorFactory _jobExecutorFactory = jobExecutorFactory;
    private readonly Dictionary<Guid, IJobExecutor> _jobExecutors = new();
    private readonly Dictionary<Guid, Task> _executionTasks = new();
    private readonly AsyncQueue<Func<CancellationToken, Task>> _backgroundTaskQueue = new();

    public async Task StartExecutionAsync(Guid executionId)
    {
        if (_jobExecutors.ContainsKey(executionId))
        {
            throw new InvalidOperationException($"Execution with id {executionId} is already being managed.");
        }

        var jobExecutor = await _jobExecutorFactory.CreateAsync(executionId);
        _backgroundTaskQueue.Enqueue((cancellationToken) => RunExecution(executionId, jobExecutor, cancellationToken));
    }

    public void CancelExecution(Guid executionId, string username)
    {
        if (!_jobExecutors.TryGetValue(executionId, out var value))
        {
            throw new InvalidOperationException($"No execution with id {executionId} is being managed.");
        }

        var executor = value;
        executor.Cancel(username);
    }

    public void CancelExecution(Guid executionId, string username, Guid stepId)
    {
        if (!_jobExecutors.TryGetValue(executionId, out var value))
        {
            throw new InvalidOperationException($"No execution with id {executionId} is being managed.");
        }

        var executor = value;
        executor.Cancel(username, stepId);
    }

    public bool IsExecutionRunning(Guid executionId) => _jobExecutors.ContainsKey(executionId);

    public async Task WaitForTaskCompleted(Guid executionId, CancellationToken cancellationToken)
    {
        if (_executionTasks.TryGetValue(executionId, out var task))
        {
            await task.WaitAsync(cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var taskDelegate in _backgroundTaskQueue.WithCancellation(stoppingToken))
            {
                _ = taskDelegate(stoppingToken);
            }
        }
        finally
        {
            await Task.WhenAll(_executionTasks.Values);
        }
    }

    private async Task RunExecution(Guid executionId, IJobExecutor jobExecutor, CancellationToken cancellationToken)
    {
        try
        {
            _jobExecutors[executionId] = jobExecutor;
            var task = jobExecutor.RunAsync(executionId, cancellationToken);
            _executionTasks[executionId] = task;
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
            _jobExecutors.Remove(executionId);
            _executionTasks.Remove(executionId);
        }
    }
}