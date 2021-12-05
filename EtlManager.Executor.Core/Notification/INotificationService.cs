using EtlManager.DataAccess.Models;

namespace EtlManager.Executor.Core.Notification;

internal interface INotificationService
{
    public Task SendCompletionNotification(Execution execution);

    public Task SendLongRunningExecutionNotification(Execution execution);
}
