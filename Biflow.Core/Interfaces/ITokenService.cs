using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface ITokenService
{
    public Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(
        AzureCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default);

    public Task ClearAsync(Guid azureCredentialId, CancellationToken cancellationToken = default);
}
