using Microsoft.AspNetCore.Http;

namespace Biflow.ExecutorProxy.Core.Authentication;

internal class UnauthorizedResult(string body) : IResult, IStatusCodeHttpResult
{
    private static int StatusCode => StatusCodes.Status401Unauthorized;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode;
        await httpContext.Response.WriteAsync(body);
    }
}
