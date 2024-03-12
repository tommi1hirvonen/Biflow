using Microsoft.Extensions.Hosting;

namespace Biflow.Executor.Core;

public interface IExecutionManager : IHostedService
{
    public void CancelExecution(Guid executionId, string username);

    public void CancelExecution(Guid executionId, string username, Guid stepId);

    public bool IsExecutionRunning(Guid executionId);

    public Task StartExecutionAsync(Guid executionId);

    public Task WaitForTaskCompleted(Guid executionId, CancellationToken cancellationToken);

    public IEnumerable<Execution> CurrentExecutions { get; }
}