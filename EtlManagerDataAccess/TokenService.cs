using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerDataAccess
{
    public class TokenService : ITokenService
    {
        private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;
        private const string AuthenticationUrl = "https://login.microsoftonline.com/";
        private readonly SemaphoreSlim _semaphore = new(1, 1); // Synchronize access by setting initial and max values to 1

        private Dictionary<Guid, Dictionary<string, (string Token, DateTimeOffset ExpiresOn)>> AccessTokens { get; } = new();
        

        public TokenService(IDbContextFactory<EtlManagerContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<string> GetTokenAsync(AppRegistration appRegistration, string resourceUrl)
        {
            await _semaphore.WaitAsync();
            try
            {
                // If the token can be found in the dictionary and it is valid.
                if (AccessTokens.TryGetValue(appRegistration.AppRegistrationId, out var tokens)
                    && tokens is not null && tokens.TryGetValue(resourceUrl, out var token) && token.ExpiresOn >= DateTimeOffset.Now.AddMinutes(5))
                {
                    return token.Token;
                }
                else
                {
                    AccessToken resultToken;

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
                        accessToken = new AccessToken(appRegistration.AppRegistrationId, resourceUrl, token_, expiresOn_);
                        context.Add(accessToken);
                        await context.SaveChangesAsync();
                        resultToken = accessToken;
                    }

                    if (!AccessTokens.ContainsKey(appRegistration.AppRegistrationId))
                    {
                        AccessTokens[appRegistration.AppRegistrationId] = new();
                    }
                    AccessTokens[appRegistration.AppRegistrationId][resourceUrl] = (resultToken.Token, resultToken.ExpiresOn);
                    return resultToken.Token;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenFromApiAsync(AppRegistration appRegistration, string resourceUrl)
        {
            var authContext = new AuthenticationContext(AuthenticationUrl + appRegistration.TenantId);
            var clientCredential = new ClientCredential(appRegistration.ClientId, appRegistration.ClientSecret);
            var result = await authContext.AcquireTokenAsync(resourceUrl, clientCredential);
            return (result.AccessToken, result.ExpiresOn);
        }

        public void Clear()
        {
            AccessTokens.Clear();
        }

    }
}
