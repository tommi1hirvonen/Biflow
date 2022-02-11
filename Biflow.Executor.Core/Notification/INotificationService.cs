using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Notification;

internal interface INotificationService
{
    public Task SendCompletionNotification(Execution execution);

    public Task SendLongRunningExecutionNotification(Execution execution);
}
