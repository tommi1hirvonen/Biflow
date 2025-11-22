using Biflow.Ui.Mediator.Queries.User;
using Microsoft.AspNetCore.Authorization;

namespace Biflow.Ui.Authentication;

internal class UserExistsRequirement : IAuthorizationRequirement
{
    public static async Task<bool> UserExistsAsync(IMediator mediator, string username)
    {
        var response = await mediator.SendAsync(new UserRolesQuery(username));
        return response.Roles.Any();
    }
}
