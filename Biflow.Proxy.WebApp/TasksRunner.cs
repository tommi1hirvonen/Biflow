using System.Collections.Concurrent;
using System.Timers;
using OneOf.Types;

namespace Biflow.Proxy.WebApp;

internal class TasksRunner<T> : IDisposable
{
    private readonly ILogger<TasksRunner<T>> _logger;
    private readonly System.Timers.Timer _timer = new(TimeSpan.FromMinutes(5));
    private readonly ConcurrentDictionary<Guid, TaskWrapper<T>> _tasks = [];

    public TasksRunner(ILogger<TasksRunner<T>> logger)
    {
        _logger = logger;
        _timer.Elapsed += RemoveCompletedTasks;
    }
    
    public Guid Run(Func<CancellationToken, Task<T>> taskDelegate)
    {
        var id = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var task = taskDelegate(cts.Token);
        var wrapper = new TaskWrapper<T>(task, cts, null);
        _tasks[id] = wrapper;
        _ = WaitAndMarkCompletedAsync(id, wrapper);
        return id;
    }

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

    public bool Cancel(Guid id)
    {
        if (!_tasks.TryGetValue(id, out var taskWrapper))
            return false;
        
        taskWrapper.CancellationTokenSource.Cancel();
        return true;
    }

    private async Task WaitAndMarkCompletedAsync(Guid id, TaskWrapper<T> taskWrapper)
    {
        try
        {
            await taskWrapper.Task;
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
                            && DateTime.Now - completed > TimeSpan.FromMinutes(60))
                .Select(x => x.Key)
                .ToArray();
            foreach (var completedTask in completedTasks)
            {
                _tasks.TryRemove(completedTask, out _);
            }
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