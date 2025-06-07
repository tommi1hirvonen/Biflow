using System.Security.Claims;
using JetBrains.Annotations;

namespace Biflow.Ui.Authentication;

/// <summary>
/// Provides access to the current user.
/// This type is registered in DI as a scoped service.
/// </summary>
[UsedImplicitly]
internal class UserService : IUserService
{
    public string? Username => User.Identity?.Name;
    
    public IEnumerable<string> Roles => User
        .FindAll(ClaimTypes.Role)
        .Select(r => r.Value);
    
    public ClaimsPrincipal User { get; set; } = new();
}
