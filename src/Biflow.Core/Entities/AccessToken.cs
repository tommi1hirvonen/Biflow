namespace Biflow.Core.Entities;

public class AccessToken(
    Guid azureCredentialId, string resourceUrl, string token, DateTimeOffset expiresOn)
{
    public Guid AzureCredentialId { get; init; } = azureCredentialId;

    public AzureCredential AzureCredential { get; init; } = null!;
    
    public string ResourceUrl { get; init; } = resourceUrl;

    public string Token { get; set; } = token;

    public DateTimeOffset ExpiresOn { get; set; } = expiresOn;
}
