using EtlManager.DataAccess.Models;

namespace EtlManager.Executor;

public interface INotificationService
{
    public Task SendCompletionNotification(Execution execution);

    public Task SendLongRunningExecutionNotification(Execution execution);
}
