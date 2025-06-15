namespace Biflow.Executor.Core.Notification.Options;

public class SmtpOptions
{
    public const string Section = "Smtp";
    
    public string? Server { get; init; }

    public bool EnableSsl { get; init; }

    public int Port { get; init; }

    public string? FromAddress { get; init; }

    public bool AnonymousAuthentication { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }
}