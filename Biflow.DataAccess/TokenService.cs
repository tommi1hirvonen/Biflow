using System.Text.Json;
using Azure.Core;
using Azure.Identity;

namespace Biflow.DataAccess;

public class TokenService<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory) : ITokenService
    where TDbContext : AppDbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory = dbContextFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1); // Synchronize access by setting initial and max values to 1
    private readonly Dictionary<TokenCacheKey, (string Token, DateTimeOffset ExpiresOn)> _accessTokens = [];

    public async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(
        ServicePrincipalCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var key = new TokenCacheKey(azureCredential.AzureCredentialId, scopes, username: "");
            // If the token can be found in the dictionary and it is valid.
            if (_accessTokens.TryGetValue(key, out var cachedToken)
                && cachedToken.ExpiresOn >= DateTimeOffset.Now.AddMinutes(5))
            {
                return (cachedToken.Token, cachedToken.ExpiresOn);
            }
            else
            {
                var resourceUrl = JsonSerializer.Serialize(scopes);
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var accessToken = await context.AccessTokens
                    .FirstOrDefaultAsync(at => at.AzureCredentialId == azureCredential.AzureCredentialId
                                               && at.ResourceUrl == resourceUrl
                                               && at.Username == "", cancellationToken);
                
                // If the token was set, but it's no longer valid => get new token from API and update the token.
                if (accessToken is not null && accessToken.ExpiresOn < DateTimeOffset.Now.AddMinutes(5)) // 5 min safety margin
                {
                    (accessToken.Token, accessToken.ExpiresOn) = await GetTokenFromApiAsync(
                        azureCredential,
                        scopes,
                        cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
                // Token was not set => create new token from API.
                else if (accessToken is null)
                {
                    var (token, expiresOn) = await GetTokenFromApiAsync(azureCredential, scopes, cancellationToken);
                    accessToken = new Core.Entities.AccessToken(
                        azureCredential.AzureCredentialId,
                        resourceUrl,
                        token,
                        expiresOn,
                        username: "");
                    context.AccessTokens.Add(accessToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
                
                _accessTokens[key] = (accessToken.Token, accessToken.ExpiresOn);
                
                return (accessToken.Token, accessToken.ExpiresOn);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenAsync(
        OrganizationalAccountCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var key = new TokenCacheKey(azureCredential.AzureCredentialId, scopes, azureCredential.Username);
            // If the token can be found in the dictionary and it is valid.
            if (_accessTokens.TryGetValue(key, out var cachedToken)
                && cachedToken.ExpiresOn >= DateTimeOffset.Now.AddMinutes(5))
            {
                return (cachedToken.Token, cachedToken.ExpiresOn);
            }
            else
            {
                var resourceUrl = JsonSerializer.Serialize(scopes);
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var accessToken = await context.AccessTokens
                    .FirstOrDefaultAsync(at => at.AzureCredentialId == azureCredential.AzureCredentialId
                                               && at.ResourceUrl == resourceUrl 
                                               && at.Username == azureCredential.Username, cancellationToken);
                
                // If the token was set, but it's no longer valid => get new token from API and update the token.
                if (accessToken is not null && accessToken.ExpiresOn < DateTimeOffset.Now.AddMinutes(5)) // 5 min safety margin
                {
                    (accessToken.Token, accessToken.ExpiresOn) = await GetTokenFromApiAsync(
                        azureCredential, scopes, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
                // Token was not set => create new token from API.
                else if (accessToken is null)
                {
                    var (token, expiresOn) = await GetTokenFromApiAsync(
                        azureCredential, scopes, cancellationToken);
                    accessToken = new Core.Entities.AccessToken(
                        azureCredential.AzureCredentialId,
                        resourceUrl,
                        token,
                        expiresOn,
                        azureCredential.Username);
                    context.AccessTokens.Add(accessToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
                
                _accessTokens[key] = (accessToken.Token, accessToken.ExpiresOn);
                
                return (accessToken.Token, accessToken.ExpiresOn);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenFromApiAsync(
        ServicePrincipalCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        var credential = new ClientSecretCredential(
            azureCredential.TenantId, azureCredential.ClientId, azureCredential.ClientSecret);
        var context = new TokenRequestContext(scopes);
        var token = await credential.GetTokenAsync(context, cancellationToken);
        return (token.Token, token.ExpiresOn);
    }
    
    private static async Task<(string Token, DateTimeOffset ExpiresOn)> GetTokenFromApiAsync(
        OrganizationalAccountCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        var credential = new UsernamePasswordCredential(
            azureCredential.Username, azureCredential.Password, azureCredential.TenantId, azureCredential.ClientId);
        var context = new TokenRequestContext(scopes);
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

internal class TokenCacheKey(
    Guid azureCredentialId, string[] scopes, string username) : IEquatable<TokenCacheKey>
{
    private Guid AzureCredentialId { get; } = azureCredentialId;
    private string Scopes { get; } = JsonSerializer.Serialize(scopes);
    private string Username { get; } = username;

    public bool Equals(TokenCacheKey? other) =>
        AzureCredentialId.Equals(other?.AzureCredentialId) &&
        Scopes == other.Scopes &&
        Username == other.Username;

    public override bool Equals(object? obj) => obj is TokenCacheKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(AzureCredentialId, Scopes, Username);
}