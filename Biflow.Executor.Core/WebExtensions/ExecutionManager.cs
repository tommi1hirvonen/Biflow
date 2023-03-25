using Biflow.Executor.Core.JobExecutor;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.WebExtensions;

public class ExecutionManager
{
    private readonly ILogger<ExecutionManager> _logger;
    private readonly IJobExecutorFactory _jobExecutorFactory;

    private Dictionary<Guid, IJobExecutor> JobExecutors { get; } = new();

    private Dictionary<Guid, Task> ExecutionTasks { get; } = new();

    public ExecutionManager(ILogger<ExecutionManager> logger, IJobExecutorFactory jobExecutorFactory)
    {
        _logger = logger;
        _jobExecutorFactory = jobExecutorFactory;
    }

    public async Task StartExecutionAsync(Guid executionId)
    {
        if (JobExecutors.ContainsKey(executionId))
        {
            throw new InvalidOperationException($"Execution with id {executionId} is already being managed.");
        }

        var jobExecutor = await _jobExecutorFactory.CreateAsync(executionId);
        _ = RunExecution(executionId, jobExecutor);
    }

    private async Task RunExecution(Guid executionId, IJobExecutor jobExecutor)
    {
        try
        {
            JobExecutors[executionId] = jobExecutor;
            var task = jobExecutor.RunAsync(executionId);
            ExecutionTasks[executionId] = task;
            await task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution with id {executionId}", executionId);
        }
        finally
        {
            JobExecutors.Remove(executionId);
            ExecutionTasks.Remove(executionId);
        }
    }

    public void CancelExecution(Guid executionId, string username)
    {
        if (!JobExecutors.ContainsKey(executionId))
        {
            throw new InvalidOperationException($"No execution with id {executionId} is being managed.");
        }

        var executor = JobExecutors[executionId];
        executor.Cancel(username);
    }

    public void CancelExecution(Guid executionId, string username, Guid stepId)
    {
        if (!JobExecutors.ContainsKey(executionId))
        {
            throw new InvalidOperationException($"No execution with id {executionId} is being managed.");
        }

        var executor = JobExecutors[executionId];
        executor.Cancel(username, stepId);
    }

    public bool IsExecutionRunning(Guid executionId) => JobExecutors.ContainsKey(executionId);

    public async Task WaitForTaskCompleted(Guid executionId, CancellationToken cancellationToken)
    {
        if (ExecutionTasks.TryGetValue(executionId, out var task))
        {
            await task.WaitAsync(cancellationToken);
        }
    }

}
