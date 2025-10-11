namespace Biflow.Executor.Core.Notification;

public interface INotificationService
{
    public Task<NotificationResponse> SendCompletionNotificationAsync(Execution execution);

    public Task<NotificationResponse> SendLongRunningExecutionNotificationAsync(Execution execution);
}
