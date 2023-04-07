using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public interface ITokenService
{
    public Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(AppRegistration appRegistration, string resourceUrl);

    public void Clear();
}
