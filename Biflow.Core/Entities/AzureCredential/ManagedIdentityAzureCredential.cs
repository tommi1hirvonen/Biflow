using System.ComponentModel.DataAnnotations;
using Azure.Core;
using Azure.Identity;

namespace Biflow.Core.Entities;

public class ManagedIdentityAzureCredential() : AzureCredential(AzureCredentialType.ManagedIdentity)
{
    [MaxLength(36)]
    public string? ClientId { get; set; }

    public override TokenCredential GetTokenCredential()
    {
        var clientId = string.IsNullOrEmpty(ClientId) ? null : ClientId;
        return new ManagedIdentityCredential(clientId);
    }
}