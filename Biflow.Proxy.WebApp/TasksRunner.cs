using System.Collections.Concurrent;
using System.Timers;
using Biflow.Proxy.WebApp.ProxyTasks;
using OneOf.Types;

namespace Biflow.Proxy.WebApp;

internal class TasksRunner<TTask, TStatus, TResult> : BackgroundService
    where TTask : IProxyTask<TStatus, TResult>
{
    private const int TaskCleanupIntervalMinutes = 5;
    private const int TaskCleanupThresholdMinutes = 60;
    
    private readonly ILogger<TasksRunner<TTask, TStatus, TResult>> _logger;
    private readonly System.Timers.Timer _timer = new(TimeSpan.FromMinutes(TaskCleanupIntervalMinutes));
    private readonly ConcurrentDictionary<Guid, TaskWrapper<TResult, TStatus>> _tasks = [];
    private readonly CancellationTokenSource _shutdownCts = new();

    public TasksRunner(ILogger<TasksRunner<TTask, TStatus, TResult>> logger)
    {
        _logger = logger;
        _timer.Elapsed += RemoveCompletedTasks;
    }
    
    public Guid Run(TTask proxyTask)
    {
        if (_shutdownCts.IsCancellationRequested)
            throw new ApplicationException("Cannot start new tasks when service shutdown is requested.");
        
        var id = Guid.NewGuid();
        _logger.LogInformation("Starting task with id {id}", id);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token);
        var task = proxyTask.RunAsync(cts.Token);
        var wrapper = new TaskWrapper<TResult, TStatus>(proxyTask, task, cts, null);
        _tasks[id] = wrapper;
        _ = WaitAndMarkCompletedAsync(id, wrapper);
        return id;
    }
    
    public TaskStatus<TResult, TStatus> GetStatus(Guid id)
    {
        if (!_tasks.TryGetValue(id, out var taskWrapper))
            return new NotFound();
        
        var task = taskWrapper.Task;

        if (task.IsCompletedSuccessfully)
        {
            TaskStatus<TResult, TStatus> result = new Result<TResult>(task.Result);
            return result;
        }
        
        if (task.IsFaulted)
            return new Error<Exception>(task.Exception);
        
        return new Running<TStatus>(taskWrapper.ProxyTask.Status);
    }
    
    public bool Cancel(Guid id)
    {
        if (!_tasks.TryGetValue(id, out var taskWrapper))
            return false;
     
        _logger.LogInformation("Cancelling task with id {id}", id);
        taskWrapper.CancellationTokenSource.Cancel();
        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken shutdownToken)
    {
        _timer.Start();
        try
        {
            await Task.Delay(-1, shutdownToken);
        }
        finally
        {
            // Canceling _shutdownCts will cause all tasks to be canceled because their
            // task-specific CancellationTokenSources are linked to it.
            await _shutdownCts.CancelAsync();
            var tasks = _tasks.Values.Select(x => x.Task).ToArray();
            await Task.WhenAll(tasks);
        }
    }

    private async Task WaitAndMarkCompletedAsync(Guid id, TaskWrapper<TResult, TStatus> taskWrapper)
    {
        try
        {
            await taskWrapper.Task;
            _logger.LogInformation("Task with id {id} completed successfully", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while awaiting for task {id}", id);
        }
        finally
        {
            _tasks[id] = taskWrapper with { CompletedAt = DateTime.Now };
        }
    }

    private void RemoveCompletedTasks(object? sender, ElapsedEventArgs args)
    {
        try
        {
            var completedTasks = _tasks
                .Where(x => x.Value.CompletedAt is { } completed
                            && DateTime.Now - completed > TimeSpan.FromMinutes(TaskCleanupThresholdMinutes))
                .Select(x => (x.Key, x.Value.CancellationTokenSource))
                .ToArray();
            foreach (var (task, cts) in completedTasks)
            {
                try
                {
                    cts.Dispose();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while disposing a completed task's CancellationTokenSource");
                }
                _tasks.TryRemove(task, out _);
            }
            _logger.LogInformation("Removed {count} completed tasks", completedTasks.Length);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while removing completed tasks");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _timer.Stop();
        _timer.Dispose();
    }
}