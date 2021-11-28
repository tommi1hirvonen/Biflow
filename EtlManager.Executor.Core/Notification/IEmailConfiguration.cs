using System.Net.Mail;

namespace EtlManager.Executor.Core.Notification;

public interface IEmailConfiguration
{
    public SmtpClient Client { get; }
    public string FromAddress { get; }
}
