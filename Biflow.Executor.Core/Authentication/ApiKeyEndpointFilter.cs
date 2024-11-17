using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Biflow.Executor.Core.Authentication;

public class ApiKeyEndpointFilter(
    IConfiguration configuration,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IMemoryCache memoryCache) : IEndpointFilter
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IMemoryCache _memoryCache = memoryCache;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // No authentication
        if (!_configuration.GetSection(AuthConstants.Authentication).Exists())
        {
            return await next(context);
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("x-api-key", out var requestApiKeyHeader)
            || requestApiKeyHeader.FirstOrDefault() is not { } requestApiKey)
        {
            return new UnauthorizedResult("API key header (x-api-key) is missing");
        }

        var apiKey = _configuration
            .GetSection(AuthConstants.Authentication)
            .GetValue<string>(AuthConstants.ApiKey);
        ArgumentNullException.ThrowIfNull(apiKey);

        // Provided API key matches with service API key from configuration.
        if (apiKey.Equals(requestApiKey))
        {
            return await next(context);
        }

        // Check if the API key was cached and is still valid.
        if (_memoryCache.TryGetValue<ApiKey>(requestApiKey, out var cachedApiKey) && cachedApiKey?.ValidTo >= DateTimeOffset.Now)
        {
            return await next(context);
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var apiKeyFromDb = await dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.Value == requestApiKey);

        switch (apiKeyFromDb)
        {
            case null:
                return new UnauthorizedResult("Invalid API key");
            case { IsRevoked: true }:
                return new UnauthorizedResult("API key has been revoked");
            case var _ when apiKeyFromDb.ValidTo < DateTimeOffset.Now:
                return new UnauthorizedResult("API key has expired");
            case var _ when apiKeyFromDb.ValidFrom > DateTimeOffset.Now:
                return new UnauthorizedResult("Invalid API key");
        }

        // Valid API key found from database.
        _memoryCache.Set(apiKeyFromDb.Value, apiKeyFromDb, TimeSpan.FromHours(1));
        return await next(context);
    }
}
