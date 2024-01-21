using System.Security.Claims;

namespace Biflow.Ui.Core.Authentication;

/// <summary>
/// Provides access to the current user.
/// This type is registered in DI as a scoped service.
/// </summary>
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
