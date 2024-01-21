using System.Security.Claims;
using System.Security.Principal;

namespace Biflow.DataAccess.Test;

internal class MockUserService : IUserService
{
    public MockUserService(string username, string role)
    {
        var identity = new GenericIdentity(username);
        var claim = new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, "Biflow");
        identity.AddClaim(claim);
        User = new ClaimsPrincipal(identity);
    }

    public ClaimsPrincipal User { get; private set; }

    public void SetUser(ClaimsPrincipal user)
    {
        if (User != user)
        {
            User = user;
        }
    }
}
