using System.Net.Mail;

namespace EtlManagerExecutor;

public interface IEmailConfiguration
{
    public SmtpClient Client { get; }
    public string FromAddress { get; }
}
