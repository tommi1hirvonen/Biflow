using System.Security.Claims;

namespace Biflow.DataAccess;

public interface IUserService
{
    public ClaimsPrincipal User { get; }

    public void SetUser(ClaimsPrincipal user);
}
