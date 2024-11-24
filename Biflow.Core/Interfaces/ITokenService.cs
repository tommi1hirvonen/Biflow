using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface ITokenService
{
    public Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(
        AppRegistration appRegistration,
        string resourceUrl,
        CancellationToken cancellationToken = default);

    public Task ClearAsync(CancellationToken cancellationToken = default);
}
