using System.Security.Claims;

namespace Biflow.Ui.Core.Authentication;

internal class UserService : IUserService
{
    public ClaimsPrincipal User { get; private set; } = new();

    public void SetUser(ClaimsPrincipal user)
    {
        if (User != user)
        {
            User = user;
        }
    }
}
