using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Biflow.Executor.Core.Notification;

internal class SmtpTest(IOptions<EmailOptions> options) : IEmailTest
{
    public async Task RunAsync(string toAddress)
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
        await client.SendMailAsync(mailMessage);
    }
}
