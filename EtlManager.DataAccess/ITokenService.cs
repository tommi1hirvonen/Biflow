using EtlManager.DataAccess.Models;

namespace EtlManager.DataAccess;

public interface ITokenService
{
    public Task<string> GetTokenAsync(AppRegistration appRegistration, string resourceUrl);

    public void Clear();
}
