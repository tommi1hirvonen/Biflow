using Microsoft.AspNetCore.Authorization;

namespace Biflow.Ui.Core;

internal class UserExistsRequirement : IAuthorizationRequirement
{
    public static async Task<bool> UserExistsAsync(UserService users, string userName)
    {
        var roles = await users.GetUserRolesAsync(userName);
        return roles.Any();
    }
}
