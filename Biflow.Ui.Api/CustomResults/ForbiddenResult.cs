namespace Biflow.Ui.Api.CustomResults;

internal class ForbiddenResult(object? body) : IResult, IStatusCodeHttpResult
{
    private static int StatusCode => StatusCodes.Status403Forbidden;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode;
        if (body is string s)
        {
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync(s);
            return;
        }
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(body);
    }
}