namespace Biflow.Executor.Core.Notification;

public interface INotificationService
{
    public Task SendCompletionNotificationAsync(Execution execution);

    public Task SendLongRunningExecutionNotificationAsync(Execution execution);
}
