using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface ITokenService
{
    public Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(AppRegistration appRegistration, string resourceUrl);

    public void Clear();
}
