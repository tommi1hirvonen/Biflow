using Microsoft.AspNetCore.Authorization;

namespace Biflow.Ui.Core;

internal class UserExistsRequirement : IAuthorizationRequirement
{
    public static async Task<bool> UserExistsAsync(IMediator mediator, string username)
    {
        var response = await mediator.SendAsync(new UserRolesQuery(username));
        return response.Roles.Any();
    }
}
