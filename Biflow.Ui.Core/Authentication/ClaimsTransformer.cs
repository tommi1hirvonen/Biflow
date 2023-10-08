using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Biflow.Ui.Core;

internal class ClaimsTransformer(IMemoryCache memoryCache, UserService users) : IClaimsTransformation
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly UserService _users = users;
    private const string Issuer = "Biflow";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var username = principal.Identity?.Name;
        if (username is null)
        {
            return principal;
        }
        if (principal.Claims.Any(c => c.Type == ClaimTypes.Role && c.Issuer == Issuer))
        {
            return principal;
        }
        // Claims might be transformed multiple times when a user is authorized.
        // Utilize IMemoryCache to store the role for a short period of time.
        var roles = await _memoryCache.GetOrCreateAsync($"{username}_Role", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(5);
            return _users.GetUserRolesAsync(username);
        });
        if (roles is null)
        {
            return principal;
        }

        var claims = roles
            .Select(role => new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, Issuer))
            .ToArray();
        var claimIdentity = new ClaimsIdentity(claims, NegotiateDefaults.AuthenticationScheme);
        principal.AddIdentity(claimIdentity);
        return principal;
    }
}
