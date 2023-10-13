using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;
using System.Security.Principal;

namespace Biflow.DataAccess.Test;

internal class MockHttpContext : HttpContext
{
    public MockHttpContext(string username, string role)
    {
        var identity = new GenericIdentity(username);
        var claim = new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, "Biflow");
        identity.AddClaim(claim);
        User = new ClaimsPrincipal(identity);
    }

    public override ClaimsPrincipal User { get; set; }

    public override IFeatureCollection Features => throw new NotImplementedException();

    public override HttpRequest Request => throw new NotImplementedException();

    public override HttpResponse Response => throw new NotImplementedException();

    public override ConnectionInfo Connection => throw new NotImplementedException();

    public override WebSocketManager WebSockets => throw new NotImplementedException();
    
    public override IDictionary<object, object?> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    
    public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    
    public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    
    public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    
    public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Abort()
    {
        throw new NotImplementedException();
    }
}
