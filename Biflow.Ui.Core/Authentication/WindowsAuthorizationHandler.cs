using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Biflow.Ui.Core;

internal class WindowsAuthorizationHandler : AuthorizationHandler<UserExistsRequirement>
{
    private readonly IMemoryCache _memoryCache;

    public WindowsAuthorizationHandler(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

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
        var exists = await _memoryCache.GetOrCreateAsync($"{userName}_Exists", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(5);
            return requirement.UserExistsAsync(userName);
        });
        if (exists)
        {
            context.Succeed(requirement);
        }
    }
}
