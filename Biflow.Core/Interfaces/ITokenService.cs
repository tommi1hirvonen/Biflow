using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface ITokenService
{
    public Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(
        AzureCredential credential, string[] scopes, CancellationToken cancellationToken = default)
    {
        return credential switch
        {
            ServicePrincipalCredential sp => GetTokenAsync(sp, scopes, cancellationToken),
            OrganizationalAccountCredential oa => GetTokenAsync(oa, scopes, cancellationToken),
            _ => throw new ArgumentException($"Unhandled credential type {credential.GetType().Name}")
        };
    }
    
    public Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(
        ServicePrincipalCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default);
    
    public Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(
        OrganizationalAccountCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default);

    public Task ClearAsync(CancellationToken cancellationToken = default);
}
