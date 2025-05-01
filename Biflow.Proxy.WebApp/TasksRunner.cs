using System.Collections.Concurrent;
using System.Timers;
using OneOf.Types;

namespace Biflow.Proxy.WebApp;

internal class TasksRunner<T> : IDisposable
{
    private const int TaskCleanupIntervalMinutes = 5;
    private const int TaskCleanupThresholdMinutes = 60;
    
    private readonly ILogger<TasksRunner<T>> _logger;
    private readonly System.Timers.Timer _timer = new(TimeSpan.FromMinutes(TaskCleanupIntervalMinutes));
    private readonly ConcurrentDictionary<Guid, TaskWrapper<T>> _tasks = [];

    public TasksRunner(ILogger<TasksRunner<T>> logger)
    {
        _logger = logger;
        _timer.Elapsed += RemoveCompletedTasks;
    }

    /// Initiates the execution of a long-running task using the provided task delegate and returns
    /// a unique identifier for tracking the task.
    /// <param name="taskDelegate">A function that represents the asynchronous task to be executed.
    /// It takes a CancellationToken as input and returns a Task of the specified type T.</param>
    /// <returns>
    /// A Guid representing the unique identifier of the newly started task.
    /// This identifier can be used to retrieve the task's status or perform operations like cancellation.
    /// </returns>
    public Guid Run(Func<CancellationToken, Task<T>> taskDelegate)
    {
        var id = Guid.NewGuid();
        _logger.LogInformation("Starting task with id {id}", id);
        var cts = new CancellationTokenSource();
        var task = taskDelegate(cts.Token);
        var wrapper = new TaskWrapper<T>(task, cts, null);
        _tasks[id] = wrapper;
        _ = WaitAndMarkCompletedAsync(id, wrapper);
        return id;
    }

    /// Retrieves the status of a task identified by the given unique identifier.
    /// <param name="id">The unique identifier of the task whose status is to be retrieved.</param>
    /// <returns>
    /// A TaskStatus object representing the current state of the task:
    /// - Result if the task completed successfully.
    /// - Error if the task failed with an exception.
    /// - Running if the task is still in progress.
    /// - NotFound if no task with the specified identifier exists.
    /// </returns>
    public TaskStatus<T> GetStatus(Guid id)
    {
        if (!_tasks.TryGetValue(id, out var taskWrapper))
            return new NotFound();
        
        var task = taskWrapper.Task;
        
        if (task.IsCompletedSuccessfully)
            return new Result<T>(task.Result);
        
        if (task.IsFaulted)
            return new Error<Exception>(task.Exception);
        
        return new Running();
    }

    /// Cancels a running task identified by the provided unique identifier.
    /// <param name="id">The unique identifier of the task to cancel.</param>
    /// <returns>
    /// True if the task was successfully canceled; false if no running task with the specified identifier exists.
    /// </returns>
    public bool Cancel(Guid id)
    {
        if (!_tasks.TryGetValue(id, out var taskWrapper))
            return false;
     
        _logger.LogInformation("Cancelling task with id {id}", id);
        taskWrapper.CancellationTokenSource.Cancel();
        return true;
    }

    private async Task WaitAndMarkCompletedAsync(Guid id, TaskWrapper<T> taskWrapper)
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
                .Select(x => x.Key)
                .ToArray();
            foreach (var completedTask in completedTasks)
            {
                _tasks.TryRemove(completedTask, out _);
            }
            _logger.LogInformation("Removed {count} completed tasks", completedTasks.Length);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while removing completed tasks");
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}