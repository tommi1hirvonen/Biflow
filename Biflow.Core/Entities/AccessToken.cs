using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class AccessToken(
    Guid azureCredentialId, string resourceUrl, string token, DateTimeOffset expiresOn)
{
    public Guid AzureCredentialId { get; set; } = azureCredentialId;

    public AzureCredential AzureCredential { get; set; } = null!;
    
    public string ResourceUrl { get; set; } = resourceUrl;

    public string Token { get; set; } = token;

    public DateTimeOffset ExpiresOn { get; set; } = expiresOn;
}
