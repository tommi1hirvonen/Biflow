using Biflow.Executor.Core.Notification;

namespace Biflow.Executor.Core.Test;

public class MockMessageDispatcher : IMessageDispatcher
{
    public Task SendMessageAsync(IEnumerable<string> recipients, string subject, string body, bool isBodyHtml,
        CancellationToken cancellationToken = default)
    {
        if (!recipients.Any())
        {
            throw new InvalidOperationException("Recipients can't be empty.");
        }
        
        ArgumentException.ThrowIfNullOrEmpty(subject);
        ArgumentException.ThrowIfNullOrEmpty(body);
        
        return Task.CompletedTask;
    }
}