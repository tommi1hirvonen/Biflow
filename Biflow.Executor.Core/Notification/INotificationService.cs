using Biflow.DataAccess.Models;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Biflow.Executor.ConsoleApp.Test")]
namespace Biflow.Executor.Core.Notification;

internal interface INotificationService
{
    public Task SendCompletionNotification(Execution execution);

    public Task SendLongRunningExecutionNotification(Execution execution);

    public Task SendNotification(IEnumerable<string> recipients, string subject, string body, CancellationToken cancellationToken);
}
