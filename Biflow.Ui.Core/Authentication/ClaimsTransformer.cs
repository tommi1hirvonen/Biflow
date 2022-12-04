using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Biflow.Ui.Core;

internal class ClaimsTransformer : IClaimsTransformation
{
    private readonly string _connectionString;

    public ClaimsTransformer(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BiflowContext");
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionString = connectionString;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var username = principal.Identity?.Name;
        if (username is null)
        {
            return principal;
        }
        if (principal.Claims.Any(c => c.Type == ClaimTypes.Role && c.Issuer == "Biflow"))
        {
            return principal;
        }
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var role = await connection.ExecuteScalarAsync<string?>("""
            SELECT TOP 1 [Role]
            FROM [biflow].[User]
            WHERE [Username] = @Username
            """, new { Username = username });
        if (role is null)
        {
            return principal;
        }
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, "Biflow")
        };
        var claimIdentity = new ClaimsIdentity(claims, NegotiateDefaults.AuthenticationScheme);
        principal.AddIdentity(claimIdentity);
        return principal;
    }
}
