using Microsoft.Extensions.Hosting;

namespace Biflow.Executor.Core;

public interface IExecutionManager : IHostedService
{
    public void CancelExecution(Guid executionId, string username);

    public void CancelExecution(Guid executionId, string username, Guid stepId);

    public bool IsExecutionRunning(Guid executionId);

    public Task StartExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var context = new OrchestrationContext(executionId, parentExecutionId: null, synchronizedExecution: false);
        return StartExecutionAsync(context, cancellationToken);
    }
    
    internal Task StartExecutionAsync(OrchestrationContext context, CancellationToken cancellationToken = default);

    public Task WaitForTaskCompleted(Guid executionId, CancellationToken cancellationToken);

    public IEnumerable<Execution> CurrentExecutions { get; }
}