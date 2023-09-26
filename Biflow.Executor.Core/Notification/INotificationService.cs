using Biflow.DataAccess.Models;

namespace Biflow.Executor.Core.Notification;

internal interface INotificationService
{
    public Task SendCompletionNotificationAsync(Execution execution);

    public Task SendLongRunningExecutionNotificationAsync(Execution execution);
}
