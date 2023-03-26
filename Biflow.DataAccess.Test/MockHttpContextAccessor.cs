using Microsoft.AspNetCore.Http;

namespace Biflow.DataAccess.Test;

internal class MockHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext HttpContext { get; set; }

    public MockHttpContextAccessor(string username, string role)
    {
        HttpContext = new MockHttpContext(username, role);
    }
}
