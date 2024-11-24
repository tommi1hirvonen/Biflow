using System.Net.Mail;
using System.Net;

namespace Biflow.Executor.Core;

internal class EmailOptions
{
    public const string EmailSettings = "EmailSettings";

    public string? SmtpServer { get; set; }

    public bool EnableSsl { get; set; }

    public int Port { get; set; }

    public string? FromAddress { get; set; }

    public bool AnonymousAuthentication { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public SmtpClient Client
    {
        get
        {
            if (AnonymousAuthentication)
            {
                return new SmtpClient(SmtpServer);
            }

            return new SmtpClient(SmtpServer)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(Username, Password),
                EnableSsl = EnableSsl,
                Port = Port
            };
        }
    }
}
