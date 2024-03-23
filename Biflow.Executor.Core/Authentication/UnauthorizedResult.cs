using Microsoft.AspNetCore.Http;

namespace Biflow.Executor.Core.Authentication;

internal class UnauthorizedResult(object? body) : IResult, IStatusCodeHttpResult
{
    public static int StatusCode => StatusCodes.Status401Unauthorized;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode;
        if (body is string s)
        {
            await httpContext.Response.WriteAsync(s);
            return;
        }
        await httpContext.Response.WriteAsJsonAsync(body);
    }
}
