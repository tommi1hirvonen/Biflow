namespace Biflow.Executor.Core.Notification;

public interface IMessageDispatcher
{
    public Task SendMessageAsync(IEnumerable<string> recipients, string subject, string body, bool isBodyHtml,
        CancellationToken cancellationToken = default);
}