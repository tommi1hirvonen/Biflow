using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Biflow.Ui.Mediator.Queries.User;

namespace Biflow.Ui.Authentication;

internal class ClaimsTransformer(IMemoryCache memoryCache, IMediator mediator) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var username = principal.Identity?.Name;
        if (username is null || principal.Claims.Any(c => c is { Type: ClaimTypes.Role, Issuer: AuthConstants.Issuer }))
        {
            return principal;
        }
        // Claims might be transformed multiple times when a user is authorized.
        // Use IMemoryCache to store the role for a short period of time.
        var roles = await memoryCache.GetOrCreateAsync($"{username}_Role", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(5);
            var response = await mediator.SendAsync(new UserRolesQuery(username));
            return response.Roles;
        });
        if (roles is null)
        {
            return principal;
        }

        var claims = roles
            .Select(role => new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, AuthConstants.Issuer))
            .ToArray();
        var claimIdentity = new ClaimsIdentity(claims, NegotiateDefaults.AuthenticationScheme);
        principal.AddIdentity(claimIdentity);
        return principal;
    }
}
