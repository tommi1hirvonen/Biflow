using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Biflow.Executor.Core.Notification;

internal class EmailDispatcher(ILogger<EmailDispatcher> logger, IOptionsMonitor<EmailOptions> options) : IMessageDispatcher
{
    private readonly ILogger<EmailDispatcher> _logger = logger;
    private readonly IOptionsMonitor<EmailOptions> _options = options;

    public async Task SendMessageAsync(IEnumerable<string> recipients, string subject, string body, bool isBodyHtml, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        SmtpClient client;
        try
        {
            client = options.Client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building email SMTP client. Check appsettings.json.");
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
            _logger.LogError(ex, "Error building email message object. Check appsettings.json.");
            throw;
        }

        foreach (var recipient in recipients)
        {
            mailMessage.Bcc.Add(recipient);
        }

        await client.SendMailAsync(mailMessage, cancellationToken);
    }
}