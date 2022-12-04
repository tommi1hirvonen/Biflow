using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Biflow.Ui.Core;
internal class WindowsAuthorizationHandler : AuthorizationHandler<UserExistsRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserExistsRequirement requirement)
    {
        var userName = context.User.Identity?.Name;
        if (userName is null)
        {
            return;
        }
        if (context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Issuer == "Biflow"))
        {
            context.Succeed(requirement);
            return;
        }
        var exists = await requirement.UserExistsAsync(userName);
        if (exists)
        {
            context.Succeed(requirement);
        }
    }
}
