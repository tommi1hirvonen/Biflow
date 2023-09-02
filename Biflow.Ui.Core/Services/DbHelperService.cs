using Biflow.Core;
using Biflow.DataAccess.Models;
using Dapper;

namespace Biflow.Ui.Core;

public class DbHelperService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public DbHelperService(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    /// <summary>
    /// Update the password for an existing user. Should only be used when the authentication mode is BuiltIn.
    /// </summary>
    /// <param name="username">Username for the account</param>
    /// <param name="password">New password</param>
    /// <exception cref="ArgumentException">If no user was found with the provided username</exception>
    public async Task UpdatePasswordAsync(string username, string password)
    {
        using var sqlConnection = _sqlConnectionFactory.Create();
        var affectedRows = await sqlConnection.ExecuteScalarAsync<int>(
            "EXEC [biflow].[UserUpdatePassword] @Username = @Username_, @Password = @Password_",
            new { Username_ = username, Password_ = password });
        if (affectedRows == 0)
        {
            throw new ArgumentException($"No user found with username {username}");
        }
    }

    public async Task AddUserAsync(User user, string password)
    {
        using var sqlConnection = _sqlConnectionFactory.Create();
        await sqlConnection.ExecuteAsync(
            "EXEC [biflow].[UserAdd] @Username = @Username_, @Password = @Password_, @Role = @Role_, @Email = @Email_",
            new { Username_ = user.Username, Password_ = password, Role_ = user.Role, Email_ = user.Email });
    }

    public async Task<string?> AuthenticateUserAsync(string username, string password)
    {
        using var sqlConnection = _sqlConnectionFactory.Create();
        var role = await sqlConnection.ExecuteScalarAsync<string?>(
            "EXEC [biflow].[UserAuthenticate] @Username = @Username_, @Password = @Password_",
            new { Username_ = username, Password_ = password });
        return role;
    }

    public async Task<string?> GetUserRoleAsync(string username)
    {
        using var sqlConnection = _sqlConnectionFactory.Create();
        return await sqlConnection.ExecuteScalarAsync<string?>("""
            SELECT TOP 1 [Role]
            FROM [biflow].[User]
            WHERE [Username] = @Username
            """, new { Username = username });
    }
}
