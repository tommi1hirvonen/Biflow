using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Biflow.Executor.Core.Notification;

internal class EmailTest(IOptions<EmailOptions> options) : IEmailTest
{
    private readonly IOptions<EmailOptions> _options = options;

    public async Task RunAsync(string toAddress)
    {
        var client = _options.Value.Client;
        ArgumentException.ThrowIfNullOrEmpty(_options.Value.FromAddress);
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.Value.FromAddress),
            Subject = "Biflow Test Mail",
            IsBodyHtml = true,
            Body = "This is a test email sent from Biflow."
        };
        mailMessage.To.Add(toAddress);
        await client.SendMailAsync(mailMessage);
    }
}
