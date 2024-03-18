using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Biflow.Executor.Core.Authentication;

public class ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly RequestDelegate _next = next;
    private readonly IConfiguration _configuration = configuration;

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
