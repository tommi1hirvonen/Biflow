using System.Net.Mail;
using System.Net;

namespace Biflow.Executor.Core;

internal class EmailOptions
{
    public const string EmailSettings = "EmailSettings";

    public string? SmtpServer { get; init; }

    public bool EnableSsl { get; init; }

    public int Port { get; init; }

    public string? FromAddress { get; init; }

    public bool AnonymousAuthentication { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

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
