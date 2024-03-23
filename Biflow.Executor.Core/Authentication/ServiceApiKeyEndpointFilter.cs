using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Biflow.Executor.Core.Authentication;

public class ServiceApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    private readonly IConfiguration _configuration = configuration;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // No authentication
        if (!_configuration.GetSection(AuthConstants.Authentication).Exists())
        {
            return await next(context);
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("x-api-key", out var requestApiKeyHeader)
            || requestApiKeyHeader.FirstOrDefault() is not string requestApiKey)
        {
            return new UnauthorizedResult("API key header (x-api-key) is missing");
        }

        var apiKey = _configuration
            .GetSection(AuthConstants.Authentication)
            .GetValue<string>(AuthConstants.ApiKey);
        ArgumentNullException.ThrowIfNull(apiKey);

        if (!apiKey.Equals(requestApiKey))
        {
            return new UnauthorizedResult("Invalid API key");
        }

        return await next(context);
    }
}
