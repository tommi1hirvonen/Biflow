using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using Biflow.Executor.Core.Notification.Options;

namespace Biflow.Executor.Core.Notification;

internal class SmtpDispatcher(ILogger<SmtpDispatcher> logger, IOptionsMonitor<SmtpOptions> optionsMonitor)
    : IMessageDispatcher
{
    public static SmtpClient CreateClientFrom(SmtpOptions options)
    {
        if (options.AnonymousAuthentication)
        {
            return new SmtpClient(options.Server);
        }
        
        return new SmtpClient(options.Server)
        {
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(options.Username, options.Password),
            EnableSsl = options.EnableSsl,
            Port = options.Port
        };
    }
    
    public Task SendMessageAsync(IEnumerable<string> recipients, string subject, string body, bool isBodyHtml,
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

        return client.SendMailAsync(mailMessage, cancellationToken);
    }
}