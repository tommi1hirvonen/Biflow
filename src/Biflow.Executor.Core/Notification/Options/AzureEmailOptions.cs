namespace Biflow.Executor.Core.Notification.Options;

public class AzureEmailOptions
{
    public const string Section = "AzureCommunicationService";
    
    public string? FromAddress { get; init; }
    
    public string? ConnectionString { get; init; }
}