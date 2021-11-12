using System.Net.Mail;

namespace EtlManager.Executor;

public interface IEmailConfiguration
{
    public SmtpClient Client { get; }
    public string FromAddress { get; }
}
