namespace Biflow.Executor.Core.Notification.Options;

public class GraphEmailOptions
{
    public const string Section = "Graph";
    
    public string? FromAddress { get; init; }
    
    public bool? UseSystemAssignedManagedIdentity { get; init; }
    
    public string? UserAssignedManagedIdentityClientId { get; init; }
    
    public GraphEmailServicePrincipal? ServicePrincipal { get; init; }
}

[UsedImplicitly]
public class GraphEmailServicePrincipal
{
    public string? TenantId { get; init; }
    
    public string? ClientId { get; init; }
    
    public string? ClientSecret { get; init; }
}