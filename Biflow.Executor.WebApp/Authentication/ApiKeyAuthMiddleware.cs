using Biflow.Executor.Core;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Executor.WebApp.Authentication;

public class ApiKeyAuthMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
{
    private readonly RequestDelegate _next = next;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory = dbContextFactory;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("x-api-key", out var requestApiKey))
        {
            context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("API key header (x-api-key) is missing");
            return;
        }

        var apiKey = _configuration
            .GetSection(AuthConstants.Authentication)
            .GetValue<string>(AuthConstants.ApiKey);
        ArgumentNullException.ThrowIfNull(apiKey);

        if (!apiKey.Equals(requestApiKey))
        {
            context.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        await _next(context);
    }
}
