using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

class UserNamesHandler : AuthorizationHandler<UserNamesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserNamesRequirement requirement)
    {
        var userName = context.User.Identity?.Name;

        if (userName is not null && requirement.UserNames.Contains(userName))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
