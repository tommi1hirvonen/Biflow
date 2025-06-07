using Biflow.Ui.Api.CustomResults;
using Microsoft.Extensions.Caching.Memory;

namespace Biflow.Ui.Api.Services;

internal class ApiKeyEndpointFilter(
    IDbContextFactory<ServiceDbContext> dbContextFactory,
    IMemoryCache memoryCache,
    IReadOnlyList<string> scopes) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userService = (UserService)context.HttpContext.RequestServices.GetRequiredService<IUserService>();
        
        if (!context.HttpContext.Request.Headers.TryGetValue("x-api-key", out var requestApiKeyHeader)
            || requestApiKeyHeader.FirstOrDefault() is not { } requestApiKey)
        {
            return new UnauthorizedResult("API key header (x-api-key) is missing");
        }

        // Check if the API key was cached and is still valid.
        if (memoryCache.TryGetValue<ApiKey>(requestApiKey, out var cachedApiKey)
            && cachedApiKey?.ValidTo >= DateTimeOffset.Now
            && scopes.All(scope => cachedApiKey.Scopes.Contains(scope)))
        {
            userService.Username = cachedApiKey.Name;
            return await next(context);
        }
        
        var cancellationToken = context.HttpContext.RequestAborted;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var apiKeyFromDb = await dbContext.ApiKeys
            .FirstOrDefaultAsync(k => k.Value == requestApiKey, cancellationToken);

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
            case var _ when scopes.Any(scope => !apiKeyFromDb.Scopes.Contains(scope)):
                var scopesAsText = string.Join(", ", scopes);
                return new ForbiddenResult($"API key does not have scopes required by endpoint: {scopesAsText}");
        }

        // Valid API key found from database.
        memoryCache.Set(apiKeyFromDb.Value, apiKeyFromDb, TimeSpan.FromMinutes(5));
        userService.Username = apiKeyFromDb.Name;
        return await next(context);
    }
}