using Azure.Core;
using Azure.Identity;
using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Biflow.DataAccess;

public class TokenService<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory) : ITokenService
    where TDbContext : AppDbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory = dbContextFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1); // Synchronize access by setting initial and max values to 1
    private readonly Dictionary<Guid, Dictionary<string, (string Token, DateTimeOffset ExpiresOn)>> _accessTokens = [];

    public async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(AppRegistration appRegistration, string resourceUrl)
    {
        await _semaphore.WaitAsync();
        try
        {
            // If the token can be found in the dictionary and it is valid.
            if (_accessTokens.TryGetValue(appRegistration.AppRegistrationId, out var tokens)
                && tokens is not null && tokens.TryGetValue(resourceUrl, out var token) && token.ExpiresOn >= DateTimeOffset.Now.AddMinutes(5))
            {
                return (token.Token, token.ExpiresOn);
            }
            else
            {
                Models.AccessToken resultToken;

                using var context = _dbContextFactory.CreateDbContext();
                var accessToken = await context.AccessTokens
                    .FirstOrDefaultAsync(at => at.AppRegistrationId == appRegistration.AppRegistrationId && at.ResourceUrl == resourceUrl);

                // If the token was set in database and it is valid, use that.
                if (accessToken is not null && accessToken.ExpiresOn >= DateTimeOffset.Now.AddMinutes(5))
                {
                    resultToken = accessToken;
                }
                // If the token was set but it's no longer valid => get new token from API and update the token in database.
                else if (accessToken is not null)
                {
                    (accessToken.Token, accessToken.ExpiresOn) = await GetTokenFromApiAsync(appRegistration, resourceUrl);
                    await context.SaveChangesAsync();
                    resultToken = accessToken;
                }
                // Token was not set => create new token from API.
                else
                {
                    (var token_, var expiresOn_) = await GetTokenFromApiAsync(appRegistration, resourceUrl);
                    accessToken = new Models.AccessToken(appRegistration.AppRegistrationId, resourceUrl, token_, expiresOn_);
                    context.Add(accessToken);
                    await context.SaveChangesAsync();
                    resultToken = accessToken;
                }

                if (!_accessTokens.TryGetValue(appRegistration.AppRegistrationId, out var value))
                {
                    value = [];
                    _accessTokens[appRegistration.AppRegistrationId] = value;
                }

                value[resourceUrl] = (resultToken.Token, resultToken.ExpiresOn);
                return (resultToken.Token, resultToken.ExpiresOn);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenFromApiAsync(AppRegistration appRegistration, string resourceUrl)
    {
        var credential = new ClientSecretCredential(appRegistration.TenantId, appRegistration.ClientId, appRegistration.ClientSecret);
        var context = new TokenRequestContext([resourceUrl]);
        var token = await credential.GetTokenAsync(context);
        return (token.Token, token.ExpiresOn);
    }

    public void Clear()
    {
        _accessTokens.Clear();
    }

}
