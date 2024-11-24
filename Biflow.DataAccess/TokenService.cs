using Azure.Core;
using Azure.Identity;

namespace Biflow.DataAccess;

public class TokenService<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory) : ITokenService
    where TDbContext : AppDbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory = dbContextFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1); // Synchronize access by setting initial and max values to 1
    private readonly Dictionary<Guid, Dictionary<string, (string Token, DateTimeOffset ExpiresOn)>> _accessTokens = [];

    public async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(
        AppRegistration appRegistration,
        string resourceUrl,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // If the token can be found in the dictionary and it is valid.
            if (_accessTokens.TryGetValue(appRegistration.AppRegistrationId, out var cachedTokens)
                && cachedTokens.TryGetValue(resourceUrl, out var cachedToken)
                && cachedToken.ExpiresOn >= DateTimeOffset.Now.AddMinutes(5))
            {
                return (cachedToken.Token, cachedToken.ExpiresOn);
            }
            else
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var accessToken = await context.AccessTokens
                    .FirstOrDefaultAsync(at =>
                            at.AppRegistrationId == appRegistration.AppRegistrationId && at.ResourceUrl == resourceUrl,
                        cancellationToken);
                
                // If the token was set, but it's no longer valid => get new token from API and update the token.
                if (accessToken is not null && accessToken.ExpiresOn < DateTimeOffset.Now.AddMinutes(5)) // 5 min safety margin
                {
                    (accessToken.Token, accessToken.ExpiresOn) = await GetTokenFromApiAsync(
                        appRegistration,
                        resourceUrl,
                        cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
                // Token was not set => create new token from API.
                else if (accessToken is null)
                {
                    var (token, expiresOn) = await GetTokenFromApiAsync(appRegistration, resourceUrl, cancellationToken);
                    accessToken = new Core.Entities.AccessToken(
                        appRegistration.AppRegistrationId,
                        resourceUrl,
                        token,
                        expiresOn);
                    context.AccessTokens.Add(accessToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                if (cachedTokens is null)
                {
                    cachedTokens = [];
                    _accessTokens[appRegistration.AppRegistrationId] = cachedTokens;
                }

                cachedTokens[resourceUrl] = (accessToken.Token, accessToken.ExpiresOn);
                return (accessToken.Token, accessToken.ExpiresOn);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenFromApiAsync(
        AppRegistration appRegistration,
        string resourceUrl,
        CancellationToken cancellationToken = default)
    {
        var credential = new ClientSecretCredential(appRegistration.TenantId, appRegistration.ClientId, appRegistration.ClientSecret);
        var context = new TokenRequestContext([resourceUrl]);
        var token = await credential.GetTokenAsync(context, cancellationToken);
        return (token.Token, token.ExpiresOn);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _accessTokens.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
