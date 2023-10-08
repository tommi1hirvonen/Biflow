using Biflow.Core;
using Biflow.DataAccess.Models;
using Dapper;
using System.Data;
using System.Text.Json;
using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui.Core;

public class UserService(ISqlConnectionFactory sqlConnectionFactory)
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory = sqlConnectionFactory;

    /// <summary>
    /// Update the password for an existing user. Should only be used when the authentication mode is BuiltIn.
    /// </summary>
    /// <param name="username">Username for the account</param>
    /// <param name="password">New password</param>
    /// <exception cref="ArgumentException">If no user was found with the provided username</exception>
    public static async Task AdminUpdatePasswordAsync(string username, string password, IDbConnection connection, IDbTransaction? transaction = null)
    {
        var hash = BC.HashPassword(password);
        var affectedRows = await connection.ExecuteAsync(
            """
            UPDATE [biflow].[User]
            SET [PasswordHash] = @PasswordHash
            WHERE [Username] = @Username
            """, new { Username = username, PasswordHash = hash }, transaction);
        if (affectedRows == 0)
        {
            throw new ArgumentException($"No user found with username {username}");
        }
    }

    public async Task AdminUpdatePasswordAsync(string username, string newPassword)
    {
        await using var connection = _sqlConnectionFactory.Create();
        await AdminUpdatePasswordAsync(username, newPassword, connection);
    }

    public async Task UpdatePasswordAsync(string username, string oldPassword, string newPassword)
    {
        await using var sqlConnection = _sqlConnectionFactory.Create();
        var result = await sqlConnection.QueryAsync<string?>(
            """
            SELECT TOP 1 [PasswordHash]
            FROM [biflow].[User]
            WHERE [Username] = @Username
            """,
            new { Username = username });
        var hash = result.FirstOrDefault() ?? throw new ApplicationException("User not found");
        var auth = BC.Verify(oldPassword, hash);
        if (!auth)
        {
            throw new ApplicationException("Incorrect old password");
        }
        await AdminUpdatePasswordAsync(username, newPassword, sqlConnection);
    }

    public async Task<IEnumerable<string>> AuthenticateUserAsync(string username, string password)
    {
        await using var sqlConnection = _sqlConnectionFactory.Create();
        var result = await sqlConnection.QueryAsync<(string?, string?)>(
            """
            SELECT TOP 1 [PasswordHash], [Roles]
            FROM [biflow].[User]
            WHERE [Username] = @Username
            """,
            new { Username = username });
        var (hash, rolesJson) = result.FirstOrDefault();
        if (hash is null || rolesJson is null)
        {
            return Enumerable.Empty<string>();
        }

        var auth = BC.Verify(password, hash);
        if (!auth)
        {
            return Enumerable.Empty<string>();
        }

        var roles = JsonSerializer.Deserialize<string[]>(rolesJson);
        return roles ?? Enumerable.Empty<string>();
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(string username)
    {
        await using var sqlConnection = _sqlConnectionFactory.Create();
        var rolesJson = await sqlConnection.ExecuteScalarAsync<string?>("""
            SELECT TOP 1 [Roles]
            FROM [biflow].[User]
            WHERE [Username] = @Username
            """, new { Username = username });
        if (string.IsNullOrWhiteSpace(rolesJson))
        {
            return Enumerable.Empty<string>();
        }
        var roles = JsonSerializer.Deserialize<string[]>(rolesJson);
        return roles ?? Enumerable.Empty<string>();
    }

    /// <summary>
    /// Ensures there is a user with Admin role added to the system database.
    /// This method is used when built-in authentication is used.
    /// </summary>
    /// <param name="username">Username for the admin user</param>
    /// <param name="password">Password for the admin user</param>
    /// <returns></returns>
    public async Task EnsureAdminUserExistsAsync(string username, string password)
    {
        await using var connection = _sqlConnectionFactory.Create();
        var roles = new string[] { Roles.Admin };
        var rolesJson = JsonSerializer.Serialize(roles);
        var hash = BC.HashPassword(password);
        var affectedRows = await connection.ExecuteAsync(
            """
            UPDATE [biflow].[User]
            SET [PasswordHash] = @PasswordHash, [Roles] = @Roles
            WHERE [Username] = @Username
            """, new { Username = username, PasswordHash = hash, Roles = rolesJson });
        if (affectedRows == 0)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO [biflow].[User] ([Username],[PasswordHash],[Roles],[CreatedDateTime],[LastModifiedDateTime]) VALUES
                (@Username, @PasswordHash, @Roles, getdate(), getdate())
                """, new { Username = username, PasswordHash = hash, Roles = rolesJson });
        }
    }

    /// <summary>
    /// Ensures there is a user with Admin role added to the system database.
    /// This method is used when built-in authentication is NOT used.
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public async Task EnsureAdminUserExistsAsync(string username)
    {
        await using var connection = _sqlConnectionFactory.Create();
        var roles = new string[] { Roles.Admin };
        var rolesJson = JsonSerializer.Serialize(roles);
        var affectedRows = await connection.ExecuteAsync(
            """
            UPDATE [biflow].[User]
            SET [Roles] = @Roles
            WHERE [Username] = @Username
            """, new { Username = username, Roles = rolesJson });
        if (affectedRows == 0)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO [biflow].[User] ([Username],[Roles],[CreatedDateTime],[LastModifiedDateTime]) VALUES
                (@Username, @Roles, getdate(), getdate())
                """, new { Username = username, Roles = rolesJson });
        }
    }
}
