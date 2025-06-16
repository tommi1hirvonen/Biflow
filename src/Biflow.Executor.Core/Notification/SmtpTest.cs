using Microsoft.Extensions.Options;
using System.Net.Mail;
using Biflow.Executor.Core.Notification.Options;

namespace Biflow.Executor.Core.Notification;

internal class SmtpTest(IOptions<SmtpOptions> options) : IEmailTest
{
    public Task RunAsync(string toAddress)
    {
        var client = SmtpDispatcher.CreateClientFrom(options.Value);
        ArgumentException.ThrowIfNullOrEmpty(options.Value.FromAddress);
        var mailMessage = new MailMessage
        {
            From = new MailAddress(options.Value.FromAddress),
            Subject = "Biflow Test Mail",
            IsBodyHtml = true,
            Body = "This is a test email sent from Biflow."
        };
        mailMessage.To.Add(toAddress);
        return client.SendMailAsync(mailMessage);
    }
}
