using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;

namespace Biflow.Ui.Core;

internal class UserExistsRequirement : IAuthorizationRequirement
{
    private readonly string _connectionString;

    public UserExistsRequirement(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> UserExistsAsync(string userName)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var result = await connection.ExecuteScalarAsync<string?>("""
            SELECT TOP 1 [Username]
            FROM [biflow].[User]
            WHERE [Username] = @Username
            """, new { Username = userName });
        return result is not null;
    }
}
