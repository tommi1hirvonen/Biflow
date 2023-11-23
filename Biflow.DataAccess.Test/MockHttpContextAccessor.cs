using Microsoft.AspNetCore.Http;

namespace Biflow.DataAccess.Test;

internal class MockHttpContextAccessor(string username, string role) : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; } = new MockHttpContext(username, role);
}
