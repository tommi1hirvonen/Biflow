using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public interface ITokenService
{
    public Task<(string Token, DateTimeOffset ExpiresOne)> GetTokenAsync(AppRegistration appRegistration, string resourceUrl);

    public void Clear();
}
