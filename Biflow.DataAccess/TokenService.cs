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
        AzureCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var key = new TokenCacheKey(azureCredential.AzureCredentialId, scopes);
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
                    .FirstOrDefaultAsync(at =>
                            at.AzureCredentialId == azureCredential.AzureCredentialId && at.ResourceUrl == resourceUrl,
                        cancellationToken);
                
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
                        expiresOn);
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
        AzureCredential azureCredential,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        TokenCredential credential = azureCredential switch
        {
            ServicePrincipalCredential sp => 
                new ClientSecretCredential(sp.TenantId, sp.ClientId, sp.ClientSecret),
            OrganizationalAccountCredential oa =>
                new UsernamePasswordCredential(oa.Username, oa.Password, oa.TenantId, oa.ClientId),
            _ => throw new ArgumentException($"Unhandled Azure credential type {azureCredential.GetType().Name}")
        };
        var context = new TokenRequestContext(scopes);
        var token = await credential.GetTokenAsync(context, cancellationToken);
        return (token.Token, token.ExpiresOn);
    }

    public async Task ClearAsync(Guid azureCredentialId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var keys = _accessTokens.Keys
                .Where(k => k.AzureCredentialId == azureCredentialId)
                .ToArray();
            foreach (var key in keys)
            {
                _accessTokens.Remove(key);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

internal class TokenCacheKey(
    Guid azureCredentialId, string[] scopes) : IEquatable<TokenCacheKey>
{
    public Guid AzureCredentialId { get; } = azureCredentialId;
    private string Scopes { get; } = JsonSerializer.Serialize(scopes);

    public bool Equals(TokenCacheKey? other) =>
        AzureCredentialId.Equals(other?.AzureCredentialId) &&
        Scopes == other.Scopes;

    public override bool Equals(object? obj) => obj is TokenCacheKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(AzureCredentialId, Scopes);
}