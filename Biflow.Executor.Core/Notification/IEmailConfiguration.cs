using System.Net.Mail;

namespace Biflow.Executor.Core.Notification;

public interface IEmailConfiguration
{
    public SmtpClient Client { get; }
    public string? FromAddress { get; }
}
