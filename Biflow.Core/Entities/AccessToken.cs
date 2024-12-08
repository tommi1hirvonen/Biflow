using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class AccessToken(
    Guid azureCredentialId, string resourceUrl, string token, DateTimeOffset expiresOn, string username)
{
    public Guid AzureCredentialId { get; set; } = azureCredentialId;

    public AzureCredential AzureCredential { get; set; } = null!;
    
    [MaxLength(256)]
    public string Username { get; set; } = username;

    [MaxLength(1000)]
    public string ResourceUrl { get; set; } = resourceUrl;

    public string Token { get; set; } = token;

    public DateTimeOffset ExpiresOn { get; set; } = expiresOn;

}
