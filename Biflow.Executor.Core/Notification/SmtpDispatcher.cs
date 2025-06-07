using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Biflow.Executor.Core.Notification;

internal class SmtpDispatcher(ILogger<SmtpDispatcher> logger, IOptionsMonitor<EmailOptions> optionsMonitor)
    : IMessageDispatcher
{
    public static SmtpClient CreateClientFrom(EmailOptions options)
    {
        if (options.AnonymousAuthentication)
        {
            return new SmtpClient(options.SmtpServer);
        }
        
        return new SmtpClient(options.SmtpServer)
        {
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(options.Username, options.Password),
            EnableSsl = options.EnableSsl,
            Port = options.Port
        };
    }
    
    public async Task SendMessageAsync(IEnumerable<string> recipients, string subject, string body, bool isBodyHtml,
        CancellationToken cancellationToken = default)
    {
        var options = optionsMonitor.CurrentValue;
        SmtpClient client;
        try
        {
            client = CreateClientFrom(options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building email SMTP client. Check appsettings.json.");
            throw;
        }

        MailMessage mailMessage;
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(options.FromAddress);
            mailMessage = new MailMessage
            {
                From = new MailAddress(options.FromAddress),
                Subject = subject,
                IsBodyHtml = isBodyHtml,
                Body = body
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error building email message object. Check appsettings.json.");
            throw;
        }

        foreach (var recipient in recipients)
        {
            mailMessage.Bcc.Add(recipient);
        }

        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}