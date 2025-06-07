namespace Biflow.Executor.Core.Notification;

internal class EmailOptions
{
    public const string EmailSettings = "EmailSettings";
    
    public string? ConnectionString { get; init; }

    public string? SmtpServer { get; init; }

    public bool EnableSsl { get; init; }

    public int Port { get; init; }

    public string? FromAddress { get; init; }

    public bool AnonymousAuthentication { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }
}
