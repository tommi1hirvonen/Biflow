using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Biflow.Ui.Authentication;

internal class UserExistsAuthorizationHandler(IMemoryCache memoryCache, IMediator mediator)
    : AuthorizationHandler<UserExistsRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, UserExistsRequirement requirement)
    {
        var userName = context.User.Identity?.Name;
        if (userName is null)
        {
            context.Fail();
            return;
        }
        if (context.User.Claims.Any(c => c is { Type: ClaimTypes.Role, Issuer: AuthConstants.Issuer }))
        {
            context.Succeed(requirement);
            return;
        }
        var exists = await memoryCache.GetOrCreateAsync($"{userName}_Exists", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(5);
            return UserExistsRequirement.UserExistsAsync(mediator, userName);
        });
        if (exists)
        {
            context.Succeed(requirement);
            return;
        }
        context.Fail();
    }
}
