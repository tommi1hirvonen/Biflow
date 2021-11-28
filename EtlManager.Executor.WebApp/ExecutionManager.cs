using EtlManager.Executor.Core.JobExecutor;
using EtlManager.Utilities;

namespace EtlManager.Executor.WebApp;

public class ExecutionManager
{
    private readonly ILogger<ExecutionManager> _logger;

    private Dictionary<Guid, IJobExecutor> JobExecutors { get; } = new();

    public ExecutionManager(ILogger<ExecutionManager> logger)
    {
        _logger = logger;
    }

    public void StartExecution(StartCommand command, IJobExecutor jobExecutor)
    {
        if (JobExecutors.ContainsKey(command.ExecutionId))
        {
            throw new InvalidOperationException($"Execution with id {command.ExecutionId} is already being managed.");
        }

        _ = RunExecution(command, jobExecutor);
    }

    private async Task RunExecution(StartCommand command, IJobExecutor jobExecutor)
    {
        try
        {
            JobExecutors[command.ExecutionId] = jobExecutor;
            await jobExecutor.RunAsync(command.ExecutionId, command.Notify, command.NotifyMe, command.NotifyMeOvertime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution with id {ExecutionId}", command.ExecutionId);
        }
        finally
        {
            JobExecutors.Remove(command.ExecutionId);
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
}
