using EtlManager.DataAccess.Models;

namespace EtlManager.Executor.Core.Notification;

internal interface INotificationService
{
    public Task SendCompletionNotification(Execution execution, bool notify, SubscriptionType? notifyMe);

    public Task SendLongRunningExecutionNotification(Execution execution, bool notify, bool notifyMeOvertime);
}
