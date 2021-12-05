using EtlManager.DataAccess.Models;

namespace EtlManager.Ui;

public interface IExecutorService
{
    public Task StartExecutionAsync(Guid executionId);

    public Task StopExecutionAsync(StepExecutionAttempt attempt, string username);

    public Task StopExecutionAsync(Execution execution, string username);
}
