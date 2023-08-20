using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Biflow.Executor.Core.Notification;

internal class EmailTest : IEmailTest
{
    private readonly IOptions<EmailOptions> _options;

    public EmailTest(IOptions<EmailOptions> options)
    {
        _options = options;
    }

    public async Task RunAsync(string toAddress)
    {
        SmtpClient client;
        try
        {
            client = _options.Value.Client;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error building email SMTP client. Check appsettings.json.\n{ex.Message}");
            return;
        }

        MailMessage mailMessage;
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_options.Value.FromAddress);
            mailMessage = new MailMessage
            {
                From = new MailAddress(_options.Value.FromAddress),
                Subject = "Biflow Test Mail",
                IsBodyHtml = true,
                Body = "This is a test email sent from Biflow."
            };
            mailMessage.To.Add(toAddress);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error building message object. Check appsettings.json.\n{ex.Message}");
            return;
        }

        await client.SendMailAsync(mailMessage);
    }
}
