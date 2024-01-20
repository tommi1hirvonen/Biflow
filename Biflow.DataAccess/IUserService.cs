using System.Security.Claims;

namespace Biflow.DataAccess;

/// <summary>
/// Contract to provide access to the current user in services.
/// </summary>
public interface IUserService
{
    public ClaimsPrincipal User { get; }

    public void SetUser(ClaimsPrincipal user);
}
