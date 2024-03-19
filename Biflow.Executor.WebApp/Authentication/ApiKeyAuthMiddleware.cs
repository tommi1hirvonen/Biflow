using Biflow.Core.Entities;
using Biflow.Executor.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Biflow.Executor.WebApp.Authentication;

public class ApiKeyAuthMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    IMemoryCache memoryCache)
{
    private readonly RequestDelegate _next = next;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;
    private readonly IMemoryCache _memoryCache = memoryCache;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("x-api-key", out var requestApiKeyHeader)
            || requestApiKeyHeader.FirstOrDefault() is not string requestApiKey)
        {
            context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("API key header (x-api-key) is missing");
            return;
        }

        var apiKey = _configuration
            .GetSection(AuthConstants.Authentication)
            .GetValue<string>(AuthConstants.ApiKey);
        ArgumentNullException.ThrowIfNull(apiKey);

        // Provided API key matches with service API key from configuration.
        if (apiKey.Equals(requestApiKey))
        {
            await _next(context);
            return;
        }

        // Check if the API key was cached and is still valid.
        if (_memoryCache.TryGetValue<ApiKey>(requestApiKey, out var cachedApiKey) && cachedApiKey?.ValidTo >= DateTimeOffset.Now)
        {
            await _next(context);
            return;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        var apiKeyFromDb = await dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.Value == requestApiKey);

        switch (apiKeyFromDb)
        {
            case null:
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Invalid API key");
                return;
            case { IsRevoked: true }:
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("API key has been revoked");
                return;
            case var key when key.ValidTo < DateTimeOffset.Now:
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("API key has expired");
                return;
            case var key when key.ValidFrom > DateTimeOffset.Now:
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Invalid API key");
                return;
            default:
                break;
        }

        // Valid API key found from database.
        _memoryCache.Set(apiKeyFromDb.Value, apiKeyFromDb, TimeSpan.FromHours(1));
        await _next(context);
        return;
    }
}
