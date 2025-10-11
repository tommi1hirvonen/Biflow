namespace Biflow.Executor.Core.Notification;

public interface INotificationMessageService
{
    public Task<string> CreateMessageBodyAsync(Execution execution, CancellationToken cancellationToken = default);
}