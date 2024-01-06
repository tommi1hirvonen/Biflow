using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Biflow.Ui.Core;

internal class WindowsAuthorizationHandler(IMemoryCache memoryCache, IMediator mediator) : AuthorizationHandler<UserExistsRequirement>
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IMediator _mediator = mediator;

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
            return UserExistsRequirement.UserExistsAsync(_mediator, userName);
        });
        if (exists)
        {
            context.Succeed(requirement);
        }
    }
}
