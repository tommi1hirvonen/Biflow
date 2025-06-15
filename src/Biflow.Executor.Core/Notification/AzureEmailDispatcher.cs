using Azure;
using Azure.Communication.Email;
using Biflow.Executor.Core.Notification.Options;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.Notification;

internal class AzureEmailDispatcher(IOptionsMonitor<AzureEmailOptions> optionsMonitor) : IMessageDispatcher
{
    public static EmailClient CreateClientFrom(AzureEmailOptions options) => new(options.ConnectionString);
    
    public Task SendMessageAsync(IEnumerable<string> recipients, string subject, string body, bool isBodyHtml,
        CancellationToken cancellationToken = default)
    {
        var options = optionsMonitor.CurrentValue;
        var client = new EmailClient(options.ConnectionString);
        var content = new EmailContent(subject)
        {
            Html = isBodyHtml ? body : string.Empty,
            PlainText = isBodyHtml ? string.Empty : body
        };
        var addresses = recipients.Select(x => new EmailAddress(x)).ToArray();
        var emailRecipients = new EmailRecipients(bcc: addresses);
        var mailMessage = new EmailMessage(
            senderAddress: options.FromAddress,
            recipients: emailRecipients,
            content: content);
        return client.SendAsync(WaitUntil.Completed, mailMessage, cancellationToken);
    }
}