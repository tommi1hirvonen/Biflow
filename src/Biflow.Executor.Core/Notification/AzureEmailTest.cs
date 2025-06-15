using Azure;
using Azure.Communication.Email;
using Biflow.Executor.Core.Notification.Options;
using Microsoft.Extensions.Options;

namespace Biflow.Executor.Core.Notification;

internal class AzureEmailTest(IOptionsMonitor<AzureEmailOptions> optionsMonitor) : IEmailTest
{
    public Task RunAsync(string toAddress)
    {
        var options = optionsMonitor.CurrentValue;
        var client = AzureEmailDispatcher.CreateClientFrom(options);
        ArgumentException.ThrowIfNullOrEmpty(options.FromAddress);
        var content = new EmailContent("Biflow Test Mail")
        {
            PlainText = "This is a test email sent from Biflow."
        };
        var mailMessage = new EmailMessage(
            senderAddress: options.FromAddress,
            recipientAddress: toAddress,
            content: content);
        return client.SendAsync(WaitUntil.Completed, mailMessage);
    }
}